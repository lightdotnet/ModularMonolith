using StarterKit.Inventory.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Inventory.Api.Application.StockLevels.Queries;

/// <summary>Cost data — the endpoint that sends this query is guarded by <c>Inventory.ViewCost</c>.</summary>
internal sealed record SearchStockValuationQuery(SearchStockValuationRequest Request)
    : IQuery<IResult<StockValuationDto>>;

internal class SearchStockValuationQueryHandler(InventoryDbContext context)
    : IQueryHandler<SearchStockValuationQuery, IResult<StockValuationDto>>
{
    public async Task<IResult<StockValuationDto>> Handle(
        SearchStockValuationQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        // Only levels that actually hold stock take part in a valuation.
        var scoped = context.StockLevels
            .AsNoTracking()
            .Where(x => x.QuantityOnHand > 0);

        if (lookup.ProductId.HasValue)
            scoped = scoped.Where(x => x.ProductId == lookup.ProductId.Value);

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        // EF Core's Sqlite provider cannot translate SUM over a decimal column, so project just the two
        // needed columns and add them up in memory; the line list below still pages in SQL.
        var totals = await scoped
            .Select(x => new { x.QuantityOnHand, x.TotalValueBase })
            .ToListAsync(cancellationToken);

        var grandTotalQuantity = totals.Sum(x => (long)x.QuantityOnHand);
        var grandTotalValue = totals.Sum(x => x.TotalValueBase);

        var paged = await scoped
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.LocationId)
            .ToPagedAsync(lookup, cancellationToken);

        var lines = paged.Records
            .Select(x => new StockValuationLineDto
            {
                ProductId = x.ProductId,
                LocationId = x.LocationId,
                QuantityOnHand = x.QuantityOnHand,
                AverageCostBase = x.AverageCostBase,
                TotalValueBase = x.TotalValueBase,
            })
            .ToList();

        return Result<StockValuationDto>.Success(new StockValuationDto
        {
            Lines = lines,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords,
            GrandTotalQuantity = grandTotalQuantity,
            GrandTotalValueBase = grandTotalValue,
        });
    }
}
