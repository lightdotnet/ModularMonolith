using StarterKit.Notifications.Contracts.Services;

namespace StarterKit.Notifications.Api.Application.Notifications.Queries;

internal sealed record CountUnreadNotificationsQuery(string UserId) : IQuery<int>;

internal class CountUnreadNotificationsQueryHandler(INotificationService notificationService)
    : IQueryHandler<CountUnreadNotificationsQuery, int>
{
    public Task<int> Handle(
        CountUnreadNotificationsQuery request,
        CancellationToken cancellationToken) =>
        notificationService.CountUnreadAsync(request.UserId);
}
