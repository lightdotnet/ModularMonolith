using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseReturns;

/// <summary><see cref="SearchQuery.SearchValue"/> matches the return number.</summary>
public record SearchPurchaseReturnRequest : SearchQuery
{
    public PurchaseReturnStatus? Status { get; set; }

    public long? SupplierId { get; set; }

    public long? GoodsReceiptId { get; set; }

    public string? LocationId { get; set; }
}
