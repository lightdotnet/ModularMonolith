using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Queries;

internal sealed record GetProductByIdQuery(string Id) : IQuery<IResult<ProductDto>>;

internal class GetProductByIdQueryHandler(CatalogDbContext context)
    : IQueryHandler<GetProductByIdQuery, IResult<ProductDto>>
{
    public async Task<IResult<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .AsNoTracking()
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<ProductDto>.NotFound($"Product {request.Id} not found");

        return Result<ProductDto>.Success(ToDto(entity));
    }

    // Hand-written mapping over the already-materialised entity, not a Mapster/EF Select
    // projection — see CatalogPricingService's doc comment for why Sku/Price/VatRate member access
    // is done in memory here.
    internal static ProductDto ToDto(Product entity) => new()
    {
        Id = entity.Id,
        CategoryId = entity.CategoryId,
        Name = entity.Name,
        Description = entity.Description,
        Sku = entity.Sku.Value,
        Price = entity.Price.Amount,
        Currency = entity.Price.Currency,
        VatRate = entity.VatRate.Value,
        Status = entity.Status,
        Images = entity.Images
            .OrderBy(x => x.SortOrder)
            .Select(x => new ProductImageDto { Url = x.Url, SortOrder = x.SortOrder })
            .ToList(),
    };
}
