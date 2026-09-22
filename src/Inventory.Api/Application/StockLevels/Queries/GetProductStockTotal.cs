using StarterKit.Inventory.Api.Data;

namespace StarterKit.Inventory.Api.Application.StockLevels.Queries;

internal sealed record GetProductStockTotalQuery(long ProductId) : IQuery<IResult<ProductStockTotalDto>>;

internal class GetProductStockTotalQueryHandler(InventoryDbContext context)
    : IQueryHandler<GetProductStockTotalQuery, IResult<ProductStockTotalDto>>
{
    public async Task<IResult<ProductStockTotalDto>> Handle(
        GetProductStockTotalQuery request,
        CancellationToken cancellationToken)
    {
        var total = await context.StockLevels
            .AsNoTracking()
            .Where(x => x.ProductId == request.ProductId)
            .GroupBy(x => x.ProductId)
            .Select(g => new ProductStockTotalDto
            {
                ProductId = request.ProductId,
                TotalQuantityOnHand = g.Sum(x => x.QuantityOnHand),
                LocationCount = g.Count(x => x.QuantityOnHand > 0),
            })
            .FirstOrDefaultAsync(cancellationToken);

        // No StockLevel rows for this product yet — not an error, just zero stock everywhere.
        return Result<ProductStockTotalDto>.Success(total ?? new ProductStockTotalDto { ProductId = request.ProductId });
    }
}
