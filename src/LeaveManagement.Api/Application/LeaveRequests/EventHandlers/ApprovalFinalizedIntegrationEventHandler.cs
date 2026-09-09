using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.LeaveManagement.Api.Data;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests.EventHandlers;

/// <summary>
/// First subscriber to Approval's <see cref="ApprovalFinalizedIntegrationEvent"/>. Reconciles the
/// local <c>LeaveRequest.Status</c> when its approval workflow reaches a terminal outcome. Delivery
/// runs in-line in the deciding scope; the periodic reconciliation sweep is the backstop for a
/// missed or failed delivery. All failures are swallowed so a reconciliation error never fails the
/// originating decide.
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

            // Ignore a late event for a superseded workflow (e.g. after a resubmit).
            if (entity.ApprovalRequestId != notification.ApprovalRequestId)
                return;

            var mapped = LeaveRequestStatusMap.MapStatus(notification.Status);

            if (entity.Status == mapped)
                return;

            entity.Status = mapped;
            await context.SaveChangesAsync(cancellationToken);
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
