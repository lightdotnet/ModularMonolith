using StarterKit.Modules.Notifications.Application.Common;

namespace StarterKit.Modules.Notifications.Application.Notifications.Queries;

internal sealed record CountUnreadNotificationsQuery(string UserId) : IQuery<int>;

internal class CountUnreadNotificationsQueryHandler(INotificationDbContext context)
    : IQueryHandler<CountUnreadNotificationsQuery, int>
{
    public Task<int> Handle(
        CountUnreadNotificationsQuery request,
        CancellationToken cancellationToken) =>
        context.Notifications
            .AsNoTracking()
            .Where(x => x.RecipientUserId == request.UserId && x.Status == NotificationStatus.None)
            .CountAsync(cancellationToken);
}
