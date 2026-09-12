using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Services;

namespace StarterKit.Catalog.Api.Services;

/// <summary>
/// Reads full <see cref="Product"/> entities and maps to <see cref="ProductPriceInfoDto"/> in memory
/// rather than projecting <c>Sku</c>/<c>Money</c>/<c>VatPercentage</c> member access directly inside
/// an EF <c>Select</c> — <c>Sku</c> is a <c>HasConversion</c>-mapped scalar, and composing further
/// member access (<c>x.Sku.Value</c>) on a converted property inside a server-translated projection
/// is not a pattern with any precedent in this repo to confirm safe; loading the entity (already
/// needed for the OwnsOne <c>Price</c>/<c>VatRate</c> table-split columns, which load automatically)
/// and mapping afterwards sidesteps the question entirely. Equality filters against <c>Sku</c> (see
/// <c>CreateProductCommandHandler</c>) are a different, well-supported case and are not affected.
/// </summary>
internal class CatalogPricingService(CatalogDbContext context) : ICatalogPricingService
{
    public async Task<ProductPriceInfoDto?> GetPriceInfoAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.Products
            .AsNoTracking()
            .Where(new ProductByIdSpec(productId))
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<ProductPriceInfoDto>> GetPriceInfoBatchAsync(
        IEnumerable<string> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.ToList();

        var entities = await context.Products
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToList();
    }

    private static ProductPriceInfoDto ToDto(Product entity) => new()
    {
        Id = entity.Id,
        Sku = entity.Sku.Value,
        Price = entity.Price.Amount,
        Currency = entity.Price.Currency,
        VatRate = entity.VatRate.Value,
        Status = entity.Status,
    };
}
