using StarterKit.Inventory.Api.Domain.StockAdjustments;

namespace StarterKit.Inventory.Api.Services;

/// <summary>A requested signed stock change, before it is turned into a ledger entry by <see cref="StockLedger"/>.</summary>
internal sealed record StockMovement(
    long ProductId,
    string LocationId,
    int QuantityDelta,
    StockAdjustmentReason Reason,
    string PerformedByUserId,
    string? Note = null,
    long? SourceOrderId = null,
    long? SourceOrderLineId = null,
    string? IdempotencyKey = null,
    long? ReversesAdjustmentId = null);
