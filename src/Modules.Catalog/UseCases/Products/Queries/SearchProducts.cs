using Mapster;
using Monolith.Catalog.Domain.Products;

namespace Monolith.Catalog.UseCases.Products.Queries;

internal class SearchProducts
{
    public record Query(ProductLookup Lookup) : IQuery<Paged<ProductDto>>;

    internal class Handler(CatalogContext context) : IQueryHandler<Query, Paged<ProductDto>>
    {
        public Task<Paged<ProductDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            return context.Set<Product>()
                .AsNoTracking()
                .Where(new ProductLookupSpec(request.Lookup))
                .ProjectToType<ProductDto>()
                .ToPagedAsync(request.Lookup, cancellationToken);
        }
    }
}
