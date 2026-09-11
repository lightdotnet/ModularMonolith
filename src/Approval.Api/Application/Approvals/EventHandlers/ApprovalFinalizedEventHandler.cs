using StarterKit.Approval.Api.Domain.Approvals;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Approval.Api.Application.Approvals.EventHandlers;

internal class ApprovalFinalizedEventHandler(
    INotificationService notificationService)
    : INotificationHandler<ApprovalFinalizedEvent>
{
    public Task Handle(
        ApprovalFinalizedEvent notification,
        CancellationToken cancellationToken) =>
        notificationService.SendAsync(
            notification.DecidedByUserId,
            null,
            notification.RequesterUserId,
            new SystemMessage
            {
                Title = $"Request {notification.Status.ToString().ToLowerInvariant()}",
                Message = $"\"{notification.Title}\" has been {notification.Status.ToString().ToLowerInvariant()}.",
                Url = notification.DeepLinkUrl,
            },
            cancellationToken);
}
