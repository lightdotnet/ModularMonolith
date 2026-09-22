using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;

namespace StarterKit.Purchasing.Api.Application.GoodsReceipts.Queries;

internal sealed record SearchGoodsReceiptsQuery(SearchGoodsReceiptRequest Request) : IQuery<PagedResult<GoodsReceiptDto>>;

internal class SearchGoodsReceiptsQueryHandler(PurchasingDbContext context)
    : IQueryHandler<SearchGoodsReceiptsQuery, PagedResult<GoodsReceiptDto>>
{
    public async Task<PagedResult<GoodsReceiptDto>> Handle(
        SearchGoodsReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.GoodsReceipts
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.PurchaseOrder)
            .AsQueryable();

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status.Value);

        if (lookup.PurchaseOrderId.HasValue)
            scoped = scoped.Where(x => x.PurchaseOrderId == lookup.PurchaseOrderId.Value);

        if (lookup.SupplierId.HasValue)
            scoped = scoped.Where(x => x.SupplierId == lookup.SupplierId.Value);

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
        {
            var term = lookup.SearchValue.Trim();
            var number = term.ToUpperInvariant();

            // Exact receipt number (converted scalar column) or a delivery note reference containing the term.
            scoped = number.Length <= GoodsReceiptNumber.MaxLength
                ? scoped.Where(x => x.ReceiptNumber == new GoodsReceiptNumber(number)
                    || (x.DeliveryNoteRef != null && x.DeliveryNoteRef.Contains(term)))
                : scoped.Where(x => x.DeliveryNoteRef != null && x.DeliveryNoteRef.Contains(term));
        }

        var paged = await scoped
            .OrderByDescending(x => x.Created)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(GetGoodsReceiptByIdQueryHandler.ToDto)
            .ToList();

        return new PagedResult<GoodsReceiptDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
