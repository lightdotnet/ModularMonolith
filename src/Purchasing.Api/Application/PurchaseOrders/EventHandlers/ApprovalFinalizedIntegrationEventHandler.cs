using Microsoft.Extensions.Logging;
using StarterKit.Approval.Contracts.Approvals;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Shared;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.EventHandlers;

/// <summary>
/// Subscribes to Approval's <see cref="ApprovalFinalizedIntegrationEvent"/> to apply the decision to the
/// local purchase order when its approval workflow reaches a terminal outcome. One of several
/// subscribers; handler ordering across modules is not guaranteed — the periodic sweep is the delivery
/// backstop for a missed or failed in-process delivery. All failures are swallowed so a reconciliation
/// error never fails the originating decide.
/// </summary>
internal sealed class ApprovalFinalizedIntegrationEventHandler(
    PurchasingDbContext context,
    IDateTime clock,
    ILogger<ApprovalFinalizedIntegrationEventHandler> logger)
    : INotificationHandler<ApprovalFinalizedIntegrationEvent>
{
    public async Task Handle(
        ApprovalFinalizedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        try
        {
            if (notification.RequestType != PurchaseOrderStatusMap.RequestType)
                return;

            if (!long.TryParse(notification.RequestId, out var purchaseOrderId))
                return;

            var order = await context.PurchaseOrders.FirstOrDefaultAsync(
                x => x.Id == purchaseOrderId,
                cancellationToken);

            if (order is null)
                return;

            // CurrentLevel is not carried by the integration event and is never read while applying an
            // outcome, so the placeholder below is safe.
            var view = new ApprovalStatusView(
                notification.ApprovalRequestId,
                notification.RequestType,
                notification.RequestId,
                notification.Status,
                CurrentLevel: 0);

            await PurchaseOrderApprovalCoordinator.ApplyOutcomeAsync(
                order,
                context,
                view,
                clock.UtcNow,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to apply approval {ApprovalRequestId} to purchase order {PurchaseOrderId}.",
                notification.ApprovalRequestId,
                notification.RequestId);
        }
    }
}
