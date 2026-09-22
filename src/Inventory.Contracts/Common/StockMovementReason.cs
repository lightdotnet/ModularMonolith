namespace StarterKit.Inventory.Contracts.Common;

public enum StockMovementReason
{
    ManualAdjustment = 0,

    OrderPlacement = 1,

    OrderCancellationRestore = 2,

    PurchaseReceipt = 3,

    TransferIn = 4,

    TransferOut = 5,

    PurchaseReturnOut = 6,

    /// <summary>Quantity-neutral change of the moving-average cost of a product at a location.</summary>
    CostRevaluation = 7,
}
