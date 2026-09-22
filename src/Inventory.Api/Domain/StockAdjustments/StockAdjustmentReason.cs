namespace StarterKit.Inventory.Api.Domain.StockAdjustments;

/// <summary>Domain-side mirror of <c>StockMovementReason</c> in Inventory.Contracts (same names and values).</summary>
public enum StockAdjustmentReason
{
    ManualAdjustment = 0,

    OrderPlacement = 1,

    OrderCancellationRestore = 2,

    PurchaseReceipt = 3,

    TransferIn = 4,

    TransferOut = 5,

    PurchaseReturnOut = 6,

    CostRevaluation = 7,
}
