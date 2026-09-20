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

    public long? SourceOrderId { get; private set; }

    public long? SourceOrderLineId { get; private set; }

    /// <summary>Caller-chosen deduplication key; unique while set, so a repeated movement can be detected and skipped.</summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>Id of the adjustment this one undoes, set only on an order-cancellation restore.</summary>
    public long? ReversesAdjustmentId { get; private set; }

    public static StockAdjustment Create(
        long productId,
        string locationId,
        int quantityDelta,
        StockAdjustmentReason reason,
        DateTimeOffset occurredAt,
        string performedByUserId,
        string? note = null,
        long? sourceOrderId = null,
        long? sourceOrderLineId = null,
        string? idempotencyKey = null,
        long? reversesAdjustmentId = null)
    {
        if (productId <= 0)
            throw Invalid(nameof(productId), "A product is required.");

        if (string.IsNullOrWhiteSpace(locationId))
            throw Invalid(nameof(locationId), "A location is required.");

        if (quantityDelta == 0)
            throw Invalid(nameof(quantityDelta), "A stock movement cannot be zero.");

        if (string.IsNullOrWhiteSpace(performedByUserId))
            throw Invalid(nameof(performedByUserId), "A performing user is required.");

        if (reason is StockAdjustmentReason.OrderPlacement or StockAdjustmentReason.OrderCancellationRestore
            && sourceOrderId is null)
        {
            throw Invalid(nameof(sourceOrderId), "A source order is required for an order-driven movement.");
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
            SourceOrderId = sourceOrderId,
            SourceOrderLineId = sourceOrderLineId,
            IdempotencyKey = idempotencyKey,
            ReversesAdjustmentId = reversesAdjustmentId,
        };
    }

    internal void ReleaseIdempotencyKey() => IdempotencyKey = null;

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
