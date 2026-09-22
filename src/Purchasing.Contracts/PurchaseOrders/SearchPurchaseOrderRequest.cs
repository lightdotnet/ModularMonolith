using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

/// <summary><see cref="SearchQuery.SearchValue"/> matches the purchase order number.</summary>
public record SearchPurchaseOrderRequest : SearchQuery
{
    public PurchaseOrderStatus? Status { get; set; }

    public long? SupplierId { get; set; }

    public string? LocationId { get; set; }
}
