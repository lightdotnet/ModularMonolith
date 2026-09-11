using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Services;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.Commands;

internal sealed record DeleteLeaveRequestCommand(
    string Id,
    string CurrentUserId,
    bool CanManage) : ICommand<IResult>;

internal class DeleteLeaveRequestCommandHandler(
    LeaveManagementDbContext context,
    LeaveRequestApprovalCoordinator coordinator,
    IApprovalService approvalService,
    ILogger<DeleteLeaveRequestCommandHandler> logger)
    : ICommandHandler<DeleteLeaveRequestCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.LeaveRequests
            .Where(new LeaveRequestByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Leave request {request.Id} not found");

        // Reconcile a possibly-stale local status against Approval before the status gate — this
        // runs for every caller (including .manage); .manage only skips the gate itself, not this.
        await coordinator.ReconcileStatusAsync(entity, cancellationToken);

        if (!request.CanManage)
        {
            if (!entity.IsOwnedBy(request.CurrentUserId))
                return Result.Error("You can only delete your own leave requests.");

            if (!entity.IsOwnerActionable)
                return Result.Error("This leave request can no longer be deleted.");
        }

        if (entity.Status == LeaveRequestStatus.Pending && entity.ApprovalRequestId is not null)
        {
            var cancel = await approvalService.CancelAsync(
                entity.ApprovalRequestId,
                entity.UserId,
                cancellationToken);

            if (!cancel.IsSuccess)
            {
                if (!request.CanManage)
                    return Result.Error("Could not withdraw the pending approval for this leave request. Please try again.");

                logger.LogWarning(
                    "Approval request {ApprovalRequestId} could not be cancelled while a manage caller deleted leave request {LeaveRequestId}; deleting the local row anyway.",
                    entity.ApprovalRequestId,
                    entity.Id);
            }
        }

        context.LeaveRequests.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
