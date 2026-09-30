using Light.Specification;
using StarterKit.Modules.Notifications.Extensions;
using StarterKit.Modules.Notifications.Persistence;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Modules.Notifications.Features.Notifications.Queries;

internal sealed record SearchNotificationsQuery(NotificationLookup Lookup)
    : IQuery<PagedResult<NotificationDto>>;

internal class SearchNotificationsQueryHandler(NotificationDbContext context)
    : IQueryHandler<SearchNotificationsQuery, PagedResult<NotificationDto>>
{
    public Task<PagedResult<NotificationDto>> Handle(
        SearchNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Lookup;

        return context.Notifications
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(lookup.ToUserId), x => x.RecipientUserId == lookup.ToUserId)
            .WhereIf(lookup.Status.HasValue, x => x.Status == lookup.Status!.Value)
            .OrderByDescending(o => o.Created)
            .MapToDto()
            .ToPagedResultAsync(lookup, cancellationToken);
    }
}
