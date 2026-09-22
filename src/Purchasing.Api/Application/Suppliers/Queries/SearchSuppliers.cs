using StarterKit.Persistence.Extensions;
using StarterKit.Purchasing.Api.Data;

namespace StarterKit.Purchasing.Api.Application.Suppliers.Queries;

internal sealed record SearchSuppliersQuery(SearchSupplierRequest Request) : IQuery<PagedResult<SupplierDto>>;

internal class SearchSuppliersQueryHandler(PurchasingDbContext context)
    : IQueryHandler<SearchSuppliersQuery, PagedResult<SupplierDto>>
{
    public async Task<PagedResult<SupplierDto>> Handle(
        SearchSuppliersQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.Suppliers
            .AsNoTracking()
            .AsQueryable();

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status.Value);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
        {
            var term = lookup.SearchValue.Trim();

            scoped = scoped.Where(x => x.Code.Contains(term.ToUpper()) || x.Name.Contains(term));
        }

        var paged = await scoped
            .OrderBy(x => x.Name)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records
            .Select(GetSupplierByIdQueryHandler.ToDto)
            .ToList();

        return new PagedResult<SupplierDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
