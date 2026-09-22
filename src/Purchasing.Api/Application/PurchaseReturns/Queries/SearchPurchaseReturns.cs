using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.PurchaseReturns;

namespace StarterKit.Purchasing.Api.Application.PurchaseReturns.Queries;

internal sealed record SearchPurchaseReturnsQuery(
    SearchPurchaseReturnRequest Request,
    bool CanViewStockCost) : IQuery<PagedResult<PurchaseReturnDto>>;

internal class SearchPurchaseReturnsQueryHandler(PurchasingDbContext context)
    : IQueryHandler<SearchPurchaseReturnsQuery, PagedResult<PurchaseReturnDto>>
{
    public async Task<PagedResult<PurchaseReturnDto>> Handle(
        SearchPurchaseReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.PurchaseReturns
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.GoodsReceipt)
            .AsQueryable();

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status.Value);

        if (lookup.SupplierId.HasValue)
            scoped = scoped.Where(x => x.SupplierId == lookup.SupplierId.Value);

        if (lookup.GoodsReceiptId.HasValue)
            scoped = scoped.Where(x => x.GoodsReceiptId == lookup.GoodsReceiptId.Value);

        if (!string.IsNullOrEmpty(lookup.LocationId))
            scoped = scoped.Where(x => x.LocationId == lookup.LocationId);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
        {
            var number = lookup.SearchValue.Trim().ToUpperInvariant();

            // Exact number match (the column is a converted scalar); a value longer than any number can
            // never match.
            scoped = number.Length <= PurchaseReturnNumber.MaxLength
                ? scoped.Where(x => x.ReturnNumber == new PurchaseReturnNumber(number))
                : scoped.Where(x => false);
        }

        var paged = await scoped
            .OrderByDescending(x => x.Created)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(x => GetPurchaseReturnByIdQueryHandler.ToDto(x, request.CanViewStockCost))
            .ToList();

        return new PagedResult<PurchaseReturnDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
