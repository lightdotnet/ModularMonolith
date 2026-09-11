using Light.Exceptions;
using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.Organization.Contracts.Services;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;

/// <summary>
/// Editing a <c>Pending</c>/<c>Rejected</c> request as its owner is treated as a resubmission:
/// Approval has no "update" primitive, only Create/Decide/Cancel, so the prior (still-pending)
/// approval request is cancelled and a fresh one is created against the edited fields and a
/// freshly-resolved approver. The old workflow is cancelled and the new one created <b>before</b>
/// any local field is touched, so a failure on either side leaves the row untouched; the final
/// local save carries its own best-effort cancel compensation. A <c>.manage</c> edit is a metadata
/// correction only (<see cref="LeaveRequest.ReviseDetails"/>) and never touches the workflow, but
/// still enforces the overlapping-period guard.
/// </summary>
internal sealed record UpdateLeaveRequestCommand(
    string Id,
    UpdateLeaveRequest Model,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal class UpdateLeaveRequestCommandHandler(
    LeaveManagementDbContext context,
    IOrgDirectoryService orgDirectoryService,
    LeaveRequestApprovalCoordinator coordinator,
    IApprovalService approvalService,
    ILogger<UpdateLeaveRequestCommandHandler> logger)
    : ICommandHandler<UpdateLeaveRequestCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.LeaveRequests
            .Where(new LeaveRequestByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Leave request {request.Id} not found");

        // Reconcile a possibly-stale local status against Approval before the authorization gate.
        await coordinator.ReconcileStatusAsync(entity, cancellationToken);

        if (!request.CanManage)
        {
            if (!entity.IsOwnedBy(request.CurrentUserId))
                return Result.Error("You can only edit your own leave requests.");

            if (!entity.IsOwnerActionable)
                return Result.Error("This leave request can no longer be edited.");
        }

        var model = request.Model;

        // DateRange's own constructor guard is the single source of truth for "end before start" —
        // Guard translates the ExceptionBase it throws into a Result instead of a duplicated
        // pre-check here.
        var periodResult = LeaveRequestApprovalCoordinator.Guard(
            () => new DateRange(model.StartDate, model.EndDate));

        if (!periodResult.IsSuccess)
            return Result.Error(periodResult.Message);

        var period = periodResult.Data;

        if (request.CanManage)
        {
            var manageOverlap = await coordinator.EnsureNoOverlapAsync(
                entity.EmployeeId,
                period,
                excludeRequestId: entity.Id,
                cancellationToken);

            if (!manageOverlap.IsSuccess)
                return Result.Error(manageOverlap.Message);

            // Defense in depth: ReviseDetails enforces no status guard of its own, so this call
            // should never throw, but a race or a future guard change surfaces as a Result.Error
            // instead of an unhandled exception.
            var reviseResult = LeaveRequestApprovalCoordinator.Guard(
                () => entity.ReviseDetails(model.LeaveType, period, model.Reason));

            if (!reviseResult.IsSuccess)
                return reviseResult;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        // Owner resubmission path.
        if (string.IsNullOrEmpty(model.ApproverEmployeeId))
            return Result.Error("Please select an approver.");

        var overlap = await coordinator.EnsureNoOverlapAsync(
            entity.EmployeeId,
            period,
            excludeRequestId: entity.Id,
            cancellationToken);

        if (!overlap.IsSuccess)
            return Result.Error(overlap.Message);

        var approverResolution = await coordinator.ResolveApproverAsync(
            entity.EmployeeId,
            model.ApproverEmployeeId,
            cancellationToken);

        if (!approverResolution.IsSuccess)
            return Result.Error(approverResolution.Message);

        // The JWT carries no name claims, so the requester's display name is resolved from their
        // Organization employee record instead — same source as the approver's own name.
        var requesterName = await orgDirectoryService.GetEmployeeNameAsync(
            entity.EmployeeId,
            cancellationToken);

        var wasPending = entity.Status == LeaveRequestStatus.Pending;

        if (wasPending && entity.ApprovalRequestId is not null)
        {
            var cancel = await approvalService.CancelAsync(
                entity.ApprovalRequestId,
                entity.UserId,
                cancellationToken);

            if (!cancel.IsSuccess)
                return Result.Error("Could not withdraw the current approval to resubmit. Please try again.");
        }

        var approvalResult = await coordinator.CreateApprovalAsync(
            entity,
            model.LeaveType,
            model.Reason,
            approverResolution.Data,
            requesterName,
            cancellationToken);

        if (!approvalResult.IsSuccess)
            return Result.Error("Failed to resubmit the leave request for approval.");

        try
        {
            // Resubmit runs inside the same compensation boundary as the save: a throw here still
            // triggers the best-effort cancel below instead of leaking an unhandled exception.
            entity.Resubmit(model.LeaveType, period, model.Reason, approvalResult.Data);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to persist resubmitted leave request {LeaveRequestId} after approval {ApprovalRequestId} was created; attempting to cancel the workflow.",
                entity.Id,
                approvalResult.Data);

            try
            {
                await approvalService.CancelAsync(
                    approvalResult.Data,
                    entity.UserId,
                    cancellationToken);
            }
            catch (Exception cancelEx)
            {
                logger.LogError(
                    cancelEx,
                    "Failed to compensate by cancelling approval {ApprovalRequestId} for the unsaved resubmitted leave request.",
                    approvalResult.Data);
            }

            return ex is ExceptionBase domainEx
                ? LeaveRequestApprovalCoordinator.ToResult(domainEx)
                : Result.Error("Could not save the leave request. Please try again.");
        }

        return Result.Success();
    }
}
