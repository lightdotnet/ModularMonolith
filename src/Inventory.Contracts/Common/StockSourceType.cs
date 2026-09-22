namespace StarterKit.Inventory.Contracts.Common;

/// <summary>Kind of business document a stock adjustment originates from; paired with a source id.</summary>
public enum StockSourceType
{
    Order = 0,

    /// <summary>Inbound goods receipt (posted through <c>ReceiveStockAsync</c>).</summary>
    GoodsReceipt = 1,

    /// <summary>Outbound leg of a stock transfer (posted through <c>IssueStockAsync</c>).</summary>
    Transfer = 2,

    /// <summary>Goods returned to a supplier (posted through <c>IssueStockAsync</c>).</summary>
    PurchaseReturn = 3,

    /// <summary>Inbound leg of a stock transfer (posted through <c>ReceiveStockAsync</c>).</summary>
    TransferReceipt = 4,
}
