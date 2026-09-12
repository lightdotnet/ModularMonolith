using StarterKit.Catalog.Contracts.Common;

namespace StarterKit.Catalog.Contracts.Products;

/// <summary>
/// Thin pricing projection returned by <c>ICatalogPricingService</c> — the cross-module read seam
/// for Orders/Inventory. Flattens <c>Money</c>/<c>VatPercentage</c> to scalars, same as
/// <see cref="ProductDto"/>.
/// </summary>
public class ProductPriceInfoDto : BaseDto
{
    public string Sku { get; set; } = null!;

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VatRate { get; set; }

    public ProductStatus Status { get; set; }
}
