using StarterKit.Modules.Notifications.Persistence;

namespace StarterKit.Modules.Notifications.Features.Notifications.Queries;

internal sealed record CountUnreadNotificationsQuery(string UserId) : IQuery<int>;

internal class CountUnreadNotificationsQueryHandler(NotificationDbContext context)
    : IQueryHandler<CountUnreadNotificationsQuery, int>
{
    public Task<int> Handle(
        CountUnreadNotificationsQuery request,
        CancellationToken cancellationToken) =>
        context.Notifications
            .AsNoTracking()
            .Where(x => x.ToUserId == request.UserId && x.Status == NotificationStatus.None)
            .CountAsync(cancellationToken);
}
