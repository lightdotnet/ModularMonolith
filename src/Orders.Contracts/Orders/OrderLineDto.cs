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
}
