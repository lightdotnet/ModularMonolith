using StarterKit.Inventory.Contracts.Common;
using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Inventory.Api.Domain.StockAdjustments;

/// <summary>
/// One immutable, append-only ledger entry recording a signed stock movement. There is no update or
/// delete path — a correction is a new entry (see <see cref="ReversesAdjustmentId"/>). The single
/// exception is <see cref="ReleaseIdempotencyKey"/>, which frees the key of an entry that has been
/// reversed so the same order line can be decremented again later.
/// </summary>
public class StockAdjustment : AuditableEntity<long>
{
    private StockAdjustment()
    {
    }

    public long ProductId { get; private set; }

    public string LocationId { get; private set; } = null!;

    public int QuantityDelta { get; private set; }

    public StockAdjustmentReason Reason { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string PerformedByUserId { get; private set; } = null!;

    /// <summary>Kind of document this entry originates from; null for a manual adjustment with no source.</summary>
    public StockSourceType? SourceType { get; private set; }

    public long? SourceId { get; private set; }

    public long? SourceLineId { get; private set; }

    /// <summary>Caller-chosen deduplication key; unique while set, so a repeated movement can be detected and skipped.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Id of the adjustment this one undoes, set only on an order-cancellation restore.</summary>
    public long? ReversesAdjustmentId { get; private set; }

    /// <summary>Base-currency unit cost of the movement (the average removed for an outbound one); 0 on pre-valuation rows.</summary>
    public decimal UnitCostBase { get; private set; }

    /// <summary>Authoritative signed value change of the movement in base currency, decimal(19,4).</summary>
    public decimal ValueDeltaBase { get; private set; }

    public static StockAdjustment Create(
        long productId,
        string locationId,
        int quantityDelta,
        StockAdjustmentReason reason,
        DateTimeOffset occurredAt,
        string performedByUserId,
        string? note = null,
        StockSourceType? sourceType = null,
        long? sourceId = null,
        long? sourceLineId = null,
        string? idempotencyKey = null,
        long? reversesAdjustmentId = null,
        decimal unitCostBase = 0m,
        decimal valueDeltaBase = 0m)
    {
        if (productId <= 0)
            throw Invalid(nameof(productId), "A product is required.");

        if (string.IsNullOrWhiteSpace(locationId))
            throw Invalid(nameof(locationId), "A location is required.");

        // A cost revaluation is the one quantity-neutral movement; every other movement must move stock.
        if (reason == StockAdjustmentReason.CostRevaluation)
        {
            if (quantityDelta != 0)
                throw Invalid(nameof(quantityDelta), "A cost revaluation cannot move quantity.");

            if (unitCostBase <= 0m)
                throw Invalid(nameof(unitCostBase), "A cost revaluation requires a positive unit cost.");
        }
        else if (quantityDelta == 0)
        {
            throw Invalid(nameof(quantityDelta), "A stock movement cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(performedByUserId))
            throw Invalid(nameof(performedByUserId), "A performing user is required.");

        if (sourceId is not null && sourceType is null)
            throw Invalid(nameof(sourceType), "A source type is required when a source id is given.");

        var requiredSourceType = RequiredSourceType(reason);

        if (requiredSourceType is not null)
        {
            if (sourceType != requiredSourceType)
                throw Invalid(nameof(sourceType), $"A {reason} movement must have the {requiredSourceType} source type.");

            if (sourceId is null)
                throw Invalid(nameof(sourceId), $"A source document is required for a {reason} movement.");
        }

        if (reason == StockAdjustmentReason.OrderCancellationRestore && reversesAdjustmentId is null)
            throw Invalid(nameof(reversesAdjustmentId), "A restore must reference the adjustment it reverses.");

        return new StockAdjustment
        {
            ProductId = productId,
            LocationId = locationId,
            QuantityDelta = quantityDelta,
            Reason = reason,
            Note = note,
            OccurredAt = occurredAt,
            PerformedByUserId = performedByUserId,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceLineId = sourceLineId,
            IdempotencyKey = idempotencyKey,
            ReversesAdjustmentId = reversesAdjustmentId,
            UnitCostBase = unitCostBase,
            ValueDeltaBase = valueDeltaBase,
        };
    }

    /// <summary>The source type a source-driven reason must carry; null for reasons with no source (manual, revaluation).</summary>
    public static StockSourceType? RequiredSourceType(StockAdjustmentReason reason) => reason switch
    {
        StockAdjustmentReason.OrderPlacement or StockAdjustmentReason.OrderCancellationRestore => StockSourceType.Order,
        StockAdjustmentReason.PurchaseReceipt => StockSourceType.GoodsReceipt,
        StockAdjustmentReason.TransferIn => StockSourceType.TransferReceipt,
        StockAdjustmentReason.TransferOut => StockSourceType.Transfer,
        StockAdjustmentReason.PurchaseReturnOut => StockSourceType.PurchaseReturn,
        _ => null,
    };

    internal void ReleaseIdempotencyKey() => IdempotencyKey = null;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
