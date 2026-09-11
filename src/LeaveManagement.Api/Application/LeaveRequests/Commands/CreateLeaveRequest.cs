using Light.Exceptions;
using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;
using StarterKit.Organization.Contracts.Services;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;

internal sealed record CreateLeaveRequestCommand(
    CreateLeaveRequest Model,
    string RequesterUserId,
    string? RequesterEmployeeId) : ICommand<IResult<string>>;

internal class CreateLeaveRequestCommandHandler(
    LeaveManagementDbContext context,
    IOrgDirectoryService orgDirectoryService,
    LeaveRequestApprovalCoordinator coordinator,
    IApprovalService approvalService,
    ILogger<CreateLeaveRequestCommandHandler> logger)
    : ICommandHandler<CreateLeaveRequestCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RequesterEmployeeId))
            return Result<string>.Error("Your account is not linked to an employee record.");

        var model = request.Model;

        // DateRange's own constructor guard is the single source of truth for "end before start" —
        // Guard translates the ExceptionBase it throws into a Result instead of a duplicated
        // pre-check here.
        var periodResult = LeaveRequestApprovalCoordinator.Guard(
            () => new DateRange(model.StartDate, model.EndDate));

        if (!periodResult.IsSuccess)
            return Result<string>.Error(periodResult.Message);

        var period = periodResult.Data;

        var overlap = await coordinator.EnsureNoOverlapAsync(
            request.RequesterEmployeeId,
            period,
            excludeRequestId: null,
            cancellationToken);

        if (!overlap.IsSuccess)
            return Result<string>.Error(overlap.Message);

        var approverResolution = await coordinator.ResolveApproverAsync(
            request.RequesterEmployeeId,
            model.ApproverEmployeeId,
            cancellationToken);

        if (!approverResolution.IsSuccess)
            return Result<string>.Error(approverResolution.Message);

        // The JWT carries no name claims, so the requester's display name is resolved from their
        // Organization employee record instead — same source as the approver's own name above.
        var requesterName = await orgDirectoryService.GetEmployeeNameAsync(
            request.RequesterEmployeeId,
            cancellationToken);

        var entityResult = LeaveRequestApprovalCoordinator.Guard(
            () => LeaveRequest.Create(
                request.RequesterUserId,
                request.RequesterEmployeeId,
                model.LeaveType,
                period,
                model.Reason));

        if (!entityResult.IsSuccess)
            return Result<string>.Error(entityResult.Message);

        var entity = entityResult.Data;

        // The approval workflow is created first so nothing is persisted locally when it fails, and
        // there is a single local commit once it succeeds.
        var approvalResult = await coordinator.CreateApprovalAsync(
            entity,
            model.LeaveType,
            model.Reason,
            approverResolution.Data,
            requesterName,
            cancellationToken);

        if (!approvalResult.IsSuccess)
            return Result<string>.Error(approvalResult.Message);

        try
        {
            // Linking runs inside the same compensation boundary as the save: a throw here (defense
            // in depth — this freshly-created entity should never fail the link guard) still
            // triggers the best-effort cancel below instead of leaking an unhandled exception.
            entity.LinkApprovalRequest(approvalResult.Data);

            await context.LeaveRequests.AddAsync(entity, cancellationToken);
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
                "Failed to persist leave request {LeaveRequestId} after approval {ApprovalRequestId} was created; attempting to cancel the workflow.",
                entity.Id,
                approvalResult.Data);

            try
            {
                await approvalService.CancelAsync(
                    approvalResult.Data,
                    request.RequesterUserId,
                    cancellationToken);
            }
            catch (Exception cancelEx)
            {
                logger.LogError(
                    cancelEx,
                    "Failed to compensate by cancelling approval {ApprovalRequestId} for the unsaved leave request.",
                    approvalResult.Data);
            }

            return ex is ExceptionBase domainEx
                ? LeaveRequestApprovalCoordinator.ToResult<string>(domainEx)
                : Result<string>.Error("Could not save the leave request. Please try again.");
        }

        return Result<string>.Success(entity.Id);
    }
}
