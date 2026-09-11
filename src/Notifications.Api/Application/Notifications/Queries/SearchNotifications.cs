using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.Application.Notifications.Queries;

internal sealed record SearchNotificationsQuery(NotificationLookup Lookup)
    : IQuery<PagedResult<NotificationDto>>;

internal class SearchNotificationsQueryHandler(INotificationService notificationService)
    : IQueryHandler<SearchNotificationsQuery, PagedResult<NotificationDto>>
{
    public Task<PagedResult<NotificationDto>> Handle(
        SearchNotificationsQuery request,
        CancellationToken cancellationToken) =>
        notificationService.GetAsync(request.Lookup);
}
