using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.LeaveManagement.Api.Data;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.EventHandlers;

/// <summary>
/// Subscribes to Approval's <see cref="ApprovalFinalizedIntegrationEvent"/> to reconcile the local
/// <c>LeaveRequest.Status</c> when its approval workflow reaches a terminal outcome. This is one of
/// the subscribers; handler ordering across modules is not guaranteed — the periodic sweep is the
/// delivery backstop for a missed or failed in-process delivery. All failures are swallowed so a
/// reconciliation error never fails the originating decide.
/// </summary>
internal sealed class ApprovalFinalizedIntegrationEventHandler(
    LeaveManagementDbContext context,
    ILogger<ApprovalFinalizedIntegrationEventHandler> logger)
    : INotificationHandler<ApprovalFinalizedIntegrationEvent>
{
    public async Task Handle(
        ApprovalFinalizedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            if (notification.RequestType != LeaveRequestStatusMap.RequestType)
                return;

            var entity = await context.LeaveRequests.FirstOrDefaultAsync(
                x => x.Id == notification.RequestId,
                cancellationToken);

            if (entity is null)
                return;

            // CurrentLevel is not carried by the integration event and is never read by
            // ApplyOutcomeAsync/ApplyApprovalOutcome, so the placeholder below is safe.
            var view = new ApprovalStatusView(
                notification.ApprovalRequestId,
                notification.RequestType,
                notification.RequestId,
                notification.Status,
                CurrentLevel: 0);

            await LeaveRequestApprovalCoordinator.ApplyOutcomeAsync(entity, context, view, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to reconcile leave request {LeaveRequestId} from approval {ApprovalRequestId}.",
                notification.RequestId,
                notification.ApprovalRequestId);
        }
    }
}
