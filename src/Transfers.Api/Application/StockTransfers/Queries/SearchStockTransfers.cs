using StarterKit.Persistence.Extensions;
using StarterKit.Transfers.Api.Data;
using StarterKit.Transfers.Api.Domain.StockTransfers;

namespace StarterKit.Transfers.Api.Application.StockTransfers.Queries;

internal sealed record SearchStockTransfersQuery(
    SearchStockTransferRequest Request,
    bool CanViewCost) : IQuery<PagedResult<StockTransferDto>>;

/// <summary>List rows carry lines (for the quantity totals) but not receipts; fetch by id for those.</summary>
internal class SearchStockTransfersQueryHandler(TransfersDbContext context)
    : IQueryHandler<SearchStockTransfersQuery, PagedResult<StockTransferDto>>
{
    public async Task<PagedResult<StockTransferDto>> Handle(
        SearchStockTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.StockTransfers
            .AsNoTracking()
            .Include(x => x.Lines)
            .AsQueryable();

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status.Value);

        if (!string.IsNullOrEmpty(lookup.SourceLocationId))
            scoped = scoped.Where(x => x.SourceLocationId == lookup.SourceLocationId);

        if (!string.IsNullOrEmpty(lookup.DestinationLocationId))
            scoped = scoped.Where(x => x.DestinationLocationId == lookup.DestinationLocationId);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
        {
            var code = lookup.SearchValue.Trim().ToUpperInvariant();

            // Exact code match (the column is a converted scalar); a value longer than any code can
            // never match.
            scoped = code.Length <= TransferCode.MaxLength
                ? scoped.Where(x => x.TransferCode == new TransferCode(code))
                : scoped.Where(x => false);
        }

        var paged = await scoped
            .OrderByDescending(x => x.Created)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(x => GetStockTransferByIdQueryHandler.ToDto(x, request.CanViewCost))
            .ToList();

        return new PagedResult<StockTransferDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
