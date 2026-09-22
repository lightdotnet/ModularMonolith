using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders.Queries;

internal sealed record SearchPurchaseOrdersQuery(SearchPurchaseOrderRequest Request) : IQuery<PagedResult<PurchaseOrderDto>>;

/// <summary>List rows carry lines (for the totals) but not receipts; fetch by id for those.</summary>
internal class SearchPurchaseOrdersQueryHandler(PurchasingDbContext context)
    : IQueryHandler<SearchPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    public async Task<PagedResult<PurchaseOrderDto>> Handle(
        SearchPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.PurchaseOrders
            .AsNoTracking()
            .Include(x => x.Lines)
            .AsQueryable();

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status.Value);

        if (lookup.SupplierId.HasValue)
            scoped = scoped.Where(x => x.SupplierId == lookup.SupplierId.Value);

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
        {
            var number = lookup.SearchValue.Trim().ToUpperInvariant();

            // Exact number match (the column is a converted scalar); a value longer than any number can
            // never match.
            scoped = number.Length <= PurchaseOrderNumber.MaxLength
                ? scoped.Where(x => x.PONumber == new PurchaseOrderNumber(number))
                : scoped.Where(x => false);
        }

        var paged = await scoped
            .OrderByDescending(x => x.Created)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(GetPurchaseOrderByIdQueryHandler.ToDto)
            .ToList();

        return new PagedResult<PurchaseOrderDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
