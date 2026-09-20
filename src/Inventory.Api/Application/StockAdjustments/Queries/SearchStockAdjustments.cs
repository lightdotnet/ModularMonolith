using StarterKit.Inventory.Api.Data;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Inventory.Api.Application.StockAdjustments.Queries;

internal sealed record SearchStockAdjustmentsQuery(SearchStockAdjustmentRequest Request)
    : IQuery<PagedResult<StockAdjustmentDto>>;

internal class SearchStockAdjustmentsQueryHandler(InventoryDbContext context)
    : IQueryHandler<SearchStockAdjustmentsQuery, PagedResult<StockAdjustmentDto>>
{
    public async Task<PagedResult<StockAdjustmentDto>> Handle(
        SearchStockAdjustmentsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.StockAdjustments
            .AsNoTracking()
            .AsQueryable();

        if (lookup.ProductId.HasValue)
            scoped = scoped.Where(x => x.ProductId == lookup.ProductId.Value);

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        if (lookup.SourceOrderId.HasValue)
            scoped = scoped.Where(x => x.SourceOrderId == lookup.SourceOrderId.Value);

        var paged = await scoped
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(x => new StockAdjustmentDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                LocationId = x.LocationId,
                QuantityDelta = x.QuantityDelta,
                Reason = (StockMovementReason)x.Reason,
                Note = x.Note,
                OccurredAt = x.OccurredAt,
                PerformedByUserId = x.PerformedByUserId,
                SourceOrderId = x.SourceOrderId,
                SourceOrderLineId = x.SourceOrderLineId,
                ReversesAdjustmentId = x.ReversesAdjustmentId,
            })
            .ToList();

        return new PagedResult<StockAdjustmentDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
