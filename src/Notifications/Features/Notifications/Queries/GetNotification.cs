using StarterKit.Modules.Notifications.Extensions;
using StarterKit.Modules.Notifications.Persistence;

namespace StarterKit.Modules.Notifications.Features.Notifications.Queries;

internal sealed record GetNotificationQuery(string UserId, string Id) : IQuery<NotificationDto?>;

internal class GetNotificationQueryHandler(NotificationDbContext context)
    : IQueryHandler<GetNotificationQuery, NotificationDto?>
{
    public Task<NotificationDto?> Handle(
        GetNotificationQuery request,
        CancellationToken cancellationToken) =>
        context.Notifications
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.ToUserId == request.UserId)
            .MapToDto()
            .SingleOrDefaultAsync(cancellationToken);
}
