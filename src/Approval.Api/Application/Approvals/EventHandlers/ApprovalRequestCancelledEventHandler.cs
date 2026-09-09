using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Approval.Api.Application.Approvals.EventHandlers;

internal class ApprovalRequestCancelledEventHandler(
    INotificationService notificationService)
    : INotificationHandler<ApprovalRequestCancelledEvent>
{
    public Task Handle(
        ApprovalRequestCancelledEvent notification,
        CancellationToken cancellationToken)
    {
        // Nothing to notify when the request was withdrawn before any level had a pending approver.
        if (string.IsNullOrEmpty(notification.CurrentApproverUserId))
            return Task.CompletedTask;

        return notificationService.SendAsync(
            notification.CancelledByUserId,
            null,
            notification.CurrentApproverUserId,
            new SystemMessage
            {
                Title = "Approval request withdrawn",
                Message = $"\"{notification.Title}\" was withdrawn by the requester.",
                Url = notification.DeepLinkUrl,
            },
            cancellationToken);
    }
}
