using StarterKit.Catalog.Contracts.Common;

namespace StarterKit.Catalog.Contracts.Products;

/// <summary>
/// <see cref="Price"/>/<see cref="Currency"/> and <see cref="VatRate"/> flatten the domain's
/// <c>Money</c>/<c>VatPercentage</c> value objects to scalar fields — mirrors how
/// <c>LeaveRequestDto</c> flattens <c>DateRange</c> to <c>StartDate</c>/<c>EndDate</c>; a Contracts
/// DTO never carries a nested value-object shape.
/// </summary>
public class ProductDto : BaseDto<long>
{
    public string CategoryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary><c>null</c> once cleared via <c>RemoveProductSkuCommand</c> — see <c>Product.Sku</c>.</summary>
    public string? Sku { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = null!;

    public decimal VatRate { get; set; }

    public ProductStatus Status { get; set; }

    public IList<ProductImageDto> Images { get; set; } = [];
}
