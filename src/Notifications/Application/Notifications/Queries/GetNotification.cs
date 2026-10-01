using StarterKit.Modules.Notifications.Application.Common;
using StarterKit.Modules.Notifications.Application.Common.Mappings;

namespace StarterKit.Modules.Notifications.Application.Notifications.Queries;

internal sealed record GetNotificationQuery(string UserId, string Id) : IQuery<NotificationDto?>;

internal class GetNotificationQueryHandler(INotificationDbContext context)
    : IQueryHandler<GetNotificationQuery, NotificationDto?>
{
    public Task<NotificationDto?> Handle(
        GetNotificationQuery request,
        CancellationToken cancellationToken) =>
        context.Notifications
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.RecipientUserId == request.UserId)
            .MapToDto()
            .SingleOrDefaultAsync(cancellationToken);
}
