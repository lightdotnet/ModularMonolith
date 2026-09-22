namespace StarterKit.Orders.Contracts.Orders;

public class OrderLineDto : BaseDto<long>
{
    public string OrderCode { get; set; } = null!;

    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public decimal VatRate { get; set; }

    public int Quantity { get; set; }

    public decimal? RequestedSalePrice { get; set; }

    public decimal DiscountAmountPerUnit { get; set; }

    public decimal DiscountPercentage { get; set; }

    /// <summary>Catalog price in <see cref="CatalogCurrency"/> before conversion; only set when it differs from the order currency.</summary>
    public decimal? CatalogUnitPrice { get; set; }

    public string? CatalogCurrency { get; set; }

    /// <summary>1 unit of <see cref="CatalogCurrency"/> = this many units of the order currency.</summary>
    public decimal? AppliedRate { get; set; }

    public DateTimeOffset? RateEffectiveFrom { get; set; }
}
