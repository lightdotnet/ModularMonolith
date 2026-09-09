using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Approval.Api.Application.Approvals.EventHandlers;

internal class ApprovalStepPendingEventHandler(
    INotificationService notificationService)
    : INotificationHandler<ApprovalStepPendingEvent>
{
    public Task Handle(
        ApprovalStepPendingEvent notification,
        CancellationToken cancellationToken) =>
        notificationService.SendAsync(
            notification.RequesterUserId,
            null,
            notification.ApproverUserId,
            new SystemMessage
            {
                Title = "Approval requested",
                Message = $"\"{notification.Title}\" is waiting for your approval.",
                Url = notification.DeepLinkUrl,
            },
            cancellationToken);
}
