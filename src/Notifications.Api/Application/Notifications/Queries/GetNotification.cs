using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.Application.Notifications.Queries;

internal sealed record GetNotificationQuery(string UserId, string Id) : IQuery<NotificationDto?>;

internal class GetNotificationQueryHandler(INotificationService notificationService)
    : IQueryHandler<GetNotificationQuery, NotificationDto?>
{
    public Task<NotificationDto?> Handle(
        GetNotificationQuery request,
        CancellationToken cancellationToken) =>
        notificationService.GetByIdAsync(request.UserId, request.Id);
}
