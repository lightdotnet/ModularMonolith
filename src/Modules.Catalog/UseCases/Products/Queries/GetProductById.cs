using Mapster;
using Monolith.Catalog.Domain.Products;

namespace Monolith.Catalog.UseCases.Products.Queries;

internal class GetProductById
{
    public record Query(string Id) : IQuery<Result<ProductDto>>;

    internal class Handler(CatalogContext context) : IQueryHandler<Query, Result<ProductDto>>
    {
        public async Task<Result<ProductDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var product = await context.Products
                .AsNoTracking()
                .Where(new ProductByIdSpec(request.Id))
                .ProjectToType<ProductDto>()
                .FirstOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                return Result.NotFound($"Product {request.Id} not found");
            }

            return product;
        }
    }
}
