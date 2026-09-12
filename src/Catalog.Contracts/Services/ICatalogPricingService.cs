using StarterKit.Catalog.Contracts.Products;

namespace StarterKit.Catalog.Contracts.Services;

/// <summary>
/// Cross-module, read-only seam for resolving product pricing — consumed by other modules (Orders,
/// Inventory) without exposing the Product aggregate or its EF internals outside this module. Same
/// role as Location's <c>ILocationDirectoryService</c>.
/// </summary>
public interface ICatalogPricingService
{
    /// <summary>Resolves pricing for a single product by id, or <c>null</c> if it does not exist.</summary>
    Task<ProductPriceInfoDto?> GetPriceInfoAsync(string productId, CancellationToken cancellationToken = default);

    /// <summary>Resolves pricing for a batch of product ids in one round trip.</summary>
    Task<IReadOnlyList<ProductPriceInfoDto>> GetPriceInfoBatchAsync(
        IEnumerable<string> productIds, CancellationToken cancellationToken = default);
}
