using StarterKit.Inventory.Contracts.Common;

namespace StarterKit.Inventory.Contracts.Stock;

public class StockAdjustmentDto : BaseDto<long>
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    public int QuantityDelta { get; set; }

    public StockMovementReason Reason { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string PerformedByUserId { get; set; } = null!;

    public long? SourceOrderId { get; set; }

    public long? SourceOrderLineId { get; set; }

    public long? ReversesAdjustmentId { get; set; }

    /// <summary>Unit cost of the movement (base currency); null unless the caller has <c>Inventory.ViewCost</c>.</summary>
    public decimal? UnitCostBase { get; set; }

    /// <summary>Signed value change of the movement (base currency); null unless the caller has <c>Inventory.ViewCost</c>.</summary>
    public decimal? ValueDeltaBase { get; set; }
}
