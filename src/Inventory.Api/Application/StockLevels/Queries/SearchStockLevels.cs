using StarterKit.Inventory.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Inventory.Api.Application.StockLevels.Queries;

/// <param name="Request">Search filters and paging.</param>
/// <param name="IncludeCost">Whether the caller may see cost fields (<c>Inventory.ViewCost</c>); they are null otherwise.</param>
internal sealed record SearchStockLevelsQuery(
    SearchStockLevelRequest Request,
    bool IncludeCost = false) : IQuery<PagedResult<StockLevelDto>>;

internal class SearchStockLevelsQueryHandler(InventoryDbContext context)
    : IQueryHandler<SearchStockLevelsQuery, PagedResult<StockLevelDto>>
{
    public async Task<PagedResult<StockLevelDto>> Handle(
        SearchStockLevelsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.StockLevels
            .AsNoTracking()
            .AsQueryable();

        if (lookup.ProductId.HasValue)
            scoped = scoped.Where(x => x.ProductId == lookup.ProductId.Value);

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        var paged = await scoped
            .OrderBy(x => x.ProductId)
            .ThenBy(x => x.LocationId)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(x => new StockLevelDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                LocationId = x.LocationId,
                QuantityOnHand = x.QuantityOnHand,
                AverageCostBase = request.IncludeCost ? x.AverageCostBase : null,
                TotalValueBase = request.IncludeCost ? x.TotalValueBase : null,
            })
            .ToList();

        return new PagedResult<StockLevelDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
