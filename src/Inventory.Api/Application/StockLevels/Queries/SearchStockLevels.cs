using StarterKit.Inventory.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Inventory.Api.Application.StockLevels.Queries;

internal sealed record SearchStockLevelsQuery(SearchStockLevelRequest Request) : IQuery<PagedResult<StockLevelDto>>;

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
            .Select(x => new StockLevelDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                LocationId = x.LocationId,
                QuantityOnHand = x.QuantityOnHand,
            })
            .ToPagedAsync(lookup, cancellationToken);

        return new PagedResult<StockLevelDto>(paged.Records, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
