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
                // The recipient is the approver, not the requester — they may not own (or even have
                // access to) the requesting module's own record at notification.DeepLinkUrl (e.g. a
                // leave request belonging to someone else). Approval's own decision surface is the
                // one page every assigned approver can always reach, regardless of request type.
                Url = $"/approvals/requests/{notification.ApprovalRequestId}",
            },
            cancellationToken);
}
