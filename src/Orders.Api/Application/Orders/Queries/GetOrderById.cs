using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.Orders;

namespace StarterKit.Orders.Api.Application.Orders.Queries;

internal sealed record GetOrderByIdQuery(long Id) : IQuery<IResult<OrderDto>>;

internal class GetOrderByIdQueryHandler(OrdersDbContext context)
    : IQueryHandler<GetOrderByIdQuery, IResult<OrderDto>>
{
    public async Task<IResult<OrderDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Fees)
            .Where(new OrderByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<OrderDto>.NotFound($"Order {request.Id} not found");

        return Result<OrderDto>.Success(ToDto(entity));
    }

    internal static OrderDto ToDto(Order entity) => new()
    {
        Id = entity.Id,
        LocationId = entity.LocationId,
        MemberId = entity.MemberId,
        OrderCode = entity.OrderCode.Value,
        ExternalReferenceCode = entity.ExternalReferenceCode,
        Status = entity.Status,
        DiscountKind = entity.Discount?.Kind,
        DiscountValue = entity.Discount?.Value,
        Subtotal = entity.Subtotal,
        DiscountAmount = entity.DiscountAmount,
        FeesTotal = entity.FeesTotal,
        Total = entity.Total,
        AmountPaid = entity.AmountPaid.Amount,
        Currency = entity.AmountPaid.Currency,
        PlacedAt = entity.PlacedAt,
        CancelledAt = entity.CancelledAt,
        FulfilledAt = entity.FulfilledAt,
        CancelledReason = entity.CancelledReason,
        Lines = entity.Lines
            .Select(x => new OrderLineDto
            {
                Id = x.Id,
                OrderCode = x.OrderCode,
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Sku = x.Sku,
                UnitPrice = x.UnitPrice.Amount,
                VatRate = x.VatRate.Value,
                Quantity = x.Quantity,
                RequestedSalePrice = x.RequestedSalePrice?.Amount,
                DiscountAmountPerUnit = x.DiscountAmountPerUnit,
                DiscountPercentage = x.DiscountPercentage,
            })
            .ToList(),
        Fees = entity.Fees
            .Select(x => new OrderFeeDto
            {
                Id = x.Id,
                OrderCode = x.OrderCode,
                Name = x.Name,
                Amount = x.Amount.Amount,
                Type = x.Type,
            })
            .ToList(),
    };
}
