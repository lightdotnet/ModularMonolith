using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Contracts.Common;

namespace StarterKit.Inventory.Api.Services;

/// <summary>
/// A requested signed stock change, before it is turned into a ledger entry by <see cref="StockLedger"/>.
/// <see cref="UnitCostBase"/> is the base-currency cost of an inbound movement (defaults to the current
/// average when omitted) or the new average of a cost revaluation; <see cref="ValueDeltaBaseOverride"/>
/// pins the exact value of a reversal so it mirrors its original instead of using the current average.
/// </summary>
internal sealed record StockMovement(
    long ProductId,
    string LocationId,
    int QuantityDelta,
    StockAdjustmentReason Reason,
    string PerformedByUserId,
    string? Note = null,
    StockSourceType? SourceType = null,
    long? SourceId = null,
    long? SourceLineId = null,
    string? IdempotencyKey = null,
    long? ReversesAdjustmentId = null,
    decimal? UnitCostBase = null,
    decimal? ValueDeltaBaseOverride = null);
