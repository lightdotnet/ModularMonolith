using StarterKit.Catalog.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Catalog.Api.Application.Products.Queries;

internal sealed record ListProductsQuery(ProductSearchRequest Request) : IQuery<PagedResult<ProductDto>>;

internal class ListProductsQueryHandler(CatalogDbContext context)
    : IQueryHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var scoped = context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(lookup.CategoryId))
            scoped = scoped.Where(x => x.CategoryId == lookup.CategoryId);

        if (lookup.Status.HasValue)
            scoped = scoped.Where(x => x.Status == lookup.Status!.Value);

        if (!string.IsNullOrWhiteSpace(lookup.SearchValue))
            scoped = scoped.Where(x => x.Name.Contains(lookup.SearchValue));

        var paged = await scoped
            .OrderByDescending(x => x.Created)
            .ToPagedAsync(lookup, cancellationToken);

        var items = paged.Records.Select(GetProductByIdQueryHandler.ToDto).ToList();

        return new PagedResult<ProductDto>(items, paged.PageNumber, paged.PageSize, paged.TotalRecords);
    }
}
