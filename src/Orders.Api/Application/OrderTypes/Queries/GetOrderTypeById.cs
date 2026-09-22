using Mapster;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.OrderTypes;

namespace StarterKit.Orders.Api.Application.OrderTypes.Queries;

internal sealed record GetOrderTypeByIdQuery(
    string Id,
    OrderTypeCategory Category) : IQuery<IResult<OrderTypeDto>>;

internal class GetOrderTypeByIdQueryHandler(OrdersDbContext context)
    : IQueryHandler<GetOrderTypeByIdQuery, IResult<OrderTypeDto>>
{
    public async Task<IResult<OrderTypeDto>> Handle(
        GetOrderTypeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.OrderTypes
            .AsNoTracking()
            .Where(new OrderTypeByIdSpec(request.Id, request.Category))
            .ProjectToType<OrderTypeDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<OrderTypeDto>.NotFound($"Order type {request.Id} ({request.Category}) not found");

        return Result<OrderTypeDto>.Success(dto);
    }
}
