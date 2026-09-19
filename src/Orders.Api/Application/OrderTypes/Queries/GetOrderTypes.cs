using Mapster;
using StarterKit.Orders.Api.Data;

namespace StarterKit.Orders.Api.Application.OrderTypes.Queries;

internal sealed record GetOrderTypesQuery(OrderTypeCategory? Category = null) : IQuery<IList<OrderTypeDto>>;

internal class GetOrderTypesQueryHandler(OrdersDbContext context)
    : IQueryHandler<GetOrderTypesQuery, IList<OrderTypeDto>>
{
    public async Task<IList<OrderTypeDto>> Handle(
        GetOrderTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.OrderTypes.AsNoTracking();

        if (request.Category is not null)
            query = query.Where(x => x.Category == request.Category);

        return await query
            .OrderBy(x => x.Name)
            .ProjectToType<OrderTypeDto>()
            .ToListAsync(cancellationToken);
    }
}
