using StarterKit.Orders.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Orders.Api.Application.Orders.Queries;

internal sealed record SearchOrdersQuery(SearchOrderRequest Request) : IQuery<PagedResult<OrderDto>>;

internal class SearchOrdersQueryHandler(OrdersDbContext context)
    : IQueryHandler<SearchOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(
        SearchOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Fees)
            .AsQueryable();

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        // A Draft order is still an editable cart, not a real sale yet — hidden from the default
        // list, but still reachable by explicitly filtering for it.
        scoped = lookup.Status.HasValue
            ? scoped.Where(x => x.Status == lookup.Status!.Value)
            : scoped.Where(x => x.Status != OrderStatus.Draft);

        var paged = await scoped
            .OrderByDescending(x => x.Created)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records.Select(GetOrderByIdQueryHandler.ToDto).ToList();

        return new PagedResult<OrderDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
