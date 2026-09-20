namespace StarterKit.Inventory.Contracts.Common;

public enum StockMovementReason
{
    ManualAdjustment = 0,

    OrderPlacement = 1,

    OrderCancellationRestore = 2,

    // Reserved for future use — not produced by any current code path.
    PurchaseReceipt = 3,

    TransferIn = 4,

    TransferOut = 5,
}
