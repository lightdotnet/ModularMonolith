using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.GoodsReceipts;

/// <summary><see cref="SearchQuery.SearchValue"/> matches the receipt number or the delivery note reference.</summary>
public record SearchGoodsReceiptRequest : SearchQuery
{
    public GoodsReceiptStatus? Status { get; set; }

    public long? PurchaseOrderId { get; set; }

    public long? SupplierId { get; set; }

    public string? LocationId { get; set; }
}
