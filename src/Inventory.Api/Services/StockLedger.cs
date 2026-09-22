using Light.Exceptions;
using StarterKit.Inventory.Api.Data;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Domain.StockLevels;
using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Exceptions;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Inventory.Api.Services;

/// <summary>Cost stored for one posted ledger entry: absolute quantity and value plus the unit cost.</summary>
internal sealed record PostedCost(
    int Quantity,
    decimal UnitCostBase,
    decimal ValueBase);

/// <summary>
/// Coordinates the ledger entry and the stock level it changes so they commit in ONE
/// <c>SaveChangesAsync</c> — keeping <c>QuantityOnHand == sum(deltas)</c>. The non-negative and
/// non-zero guards stay on <see cref="StockLevel"/> / <see cref="StockAdjustment"/>; this class only
/// sequences them. A concurrent write (stale level token, or a racing first insert hitting the unique
/// index) is retried once from a clean change tracker.
/// </summary>
internal sealed class StockLedger(
    InventoryDbContext context,
    IDateTime clock)
{
    public Task ApplyAsync(
        IReadOnlyList<StockMovement> movements,
        CancellationToken cancellationToken) =>
        ExecuteWithRetryAsync(() => ApplyOnceAsync(movements, cancellationToken));

    /// <summary>
    /// Writes a reversal for every order-placement adjustment of the order that has not been reversed
    /// yet, and releases the original's idempotency key so the order can be decremented again.
    /// </summary>
    public Task ReverseOrderPlacementsAsync(
        long orderId,
        string performedByUserId,
        CancellationToken cancellationToken,
        DateTimeOffset? postedBefore = null) =>
        ExecuteWithRetryAsync(() => ReverseOnceAsync(orderId, performedByUserId, postedBefore, cancellationToken));

    /// <summary>
    /// The subset of <paramref name="candidateSourceIds"/> (of the given source type) that still hold at
    /// least one posted, unreversed adjustment written at or before <paramref name="postedBefore"/>.
    /// </summary>
    public async Task<IReadOnlyList<long>> FilterSourceIdsWithUnreversedPostingsAsync(
        StockSourceType sourceType,
        IReadOnlyCollection<long> candidateSourceIds,
        DateTimeOffset postedBefore,
        CancellationToken cancellationToken)
    {
        if (candidateSourceIds.Count == 0)
            return [];

        var postingReason = sourceType switch
        {
            StockSourceType.Order => StockAdjustmentReason.OrderPlacement,
            StockSourceType.GoodsReceipt => StockAdjustmentReason.PurchaseReceipt,
            StockSourceType.PurchaseReturn => StockAdjustmentReason.PurchaseReturnOut,
            StockSourceType.Transfer => StockAdjustmentReason.TransferOut,
            StockSourceType.TransferReceipt => StockAdjustmentReason.TransferIn,
            _ => throw new ArgumentOutOfRangeException(
                nameof(sourceType),
                sourceType,
                "No posting reason is defined for this source type."),
        };

        var candidates = candidateSourceIds.ToList();

        return await context.StockAdjustments
            .AsNoTracking()
            .Where(x => x.SourceType == sourceType
                && x.Reason == postingReason
                && x.OccurredAt <= postedBefore
                && x.SourceId != null
                && candidates.Contains(x.SourceId.Value)
                && !context.StockAdjustments.Any(r => r.ReversesAdjustmentId == x.Id))
            .Select(x => x.SourceId!.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    /// <summary>The cost postings stored under the given idempotency keys, keyed by idempotency key.</summary>
    public async Task<IReadOnlyDictionary<string, PostedCost>> GetPostedAsync(
        IReadOnlyCollection<string> idempotencyKeys,
        CancellationToken cancellationToken)
    {
        if (idempotencyKeys.Count == 0)
            return new Dictionary<string, PostedCost>();

        var keys = idempotencyKeys.ToList();

        var rows = await context.StockAdjustments
            .AsNoTracking()
            .Where(x => keys.Contains(x.IdempotencyKey!))
            .Select(x => new
            {
                x.IdempotencyKey,
                x.QuantityDelta,
                x.UnitCostBase,
                x.ValueDeltaBase,
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.IdempotencyKey!,
            x => new PostedCost(
                Math.Abs(x.QuantityDelta),
                x.UnitCostBase,
                Math.Abs(x.ValueDeltaBase)));
    }

    private async Task ApplyOnceAsync(
        IReadOnlyList<StockMovement> movements,
        CancellationToken cancellationToken)
    {
        var keys = movements
            .Where(x => x.IdempotencyKey is not null)
            .Select(x => x.IdempotencyKey!)
            .ToList();

        var existingKeys = keys.Count == 0
            ? []
            : await context.StockAdjustments
                .AsNoTracking()
                .Where(x => keys.Contains(x.IdempotencyKey!))
                .Select(x => x.IdempotencyKey!)
                .ToListAsync(cancellationToken);

        var pending = movements
            .Where(x => x.IdempotencyKey is null || !existingKeys.Contains(x.IdempotencyKey))
            .ToList();

        if (pending.Count == 0)
            return;

        await ApplyPendingAsync(pending, cancellationToken);
    }

    private async Task ReverseOnceAsync(
        long orderId,
        string performedByUserId,
        DateTimeOffset? postedBefore,
        CancellationToken cancellationToken)
    {
        var query = context.StockAdjustments
            .Where(x => x.SourceType == StockSourceType.Order
                && x.SourceId == orderId
                && x.Reason == StockAdjustmentReason.OrderPlacement);

        if (postedBefore.HasValue)
            query = query.Where(x => x.OccurredAt <= postedBefore.Value);

        var originals = await query.ToListAsync(cancellationToken);

        if (originals.Count == 0)
            return;

        var originalIds = originals.Select(x => x.Id).ToList();

        var reversedIds = await context.StockAdjustments
            .AsNoTracking()
            .Where(x => x.ReversesAdjustmentId != null && originalIds.Contains(x.ReversesAdjustmentId.Value))
            .Select(x => x.ReversesAdjustmentId!.Value)
            .ToListAsync(cancellationToken);

        var toReverse = originals
            .Where(x => !reversedIds.Contains(x.Id))
            .ToList();

        if (toReverse.Count == 0)
            return;

        var movements = new List<StockMovement>();

        foreach (var original in toReverse)
        {
            original.ReleaseIdempotencyKey();

            movements.Add(new StockMovement(
                original.ProductId,
                original.LocationId,
                -original.QuantityDelta,
                StockAdjustmentReason.OrderCancellationRestore,
                performedByUserId,
                SourceType: StockSourceType.Order,
                SourceId: orderId,
                SourceLineId: original.SourceLineId,
                IdempotencyKey: $"order-restore:{orderId}:{original.SourceLineId}:{original.Id}",
                ReversesAdjustmentId: original.Id,
                UnitCostBase: original.UnitCostBase,
                ValueDeltaBaseOverride: -original.ValueDeltaBase));
        }

        await ApplyPendingAsync(movements, cancellationToken);
    }

    /// <remarks>
    /// Limitation: a batch must not mix inbound and outbound movements for the same product/location.
    /// Only the NET delta per group is availability-checked, so an in-batch outbound is not validated
    /// against stock that an inbound in the same batch supplies, and pricing order inside the group is
    /// an implementation detail. Also, a <c>CostRevaluation</c> on a level that does not exist yet is
    /// rejected even if an inbound in the same batch would have created stock.
    /// </remarks>
    private async Task ApplyPendingAsync(
        IReadOnlyList<StockMovement> pending,
        CancellationToken cancellationToken)
    {
        var groups = pending
            .Select((movement, index) => (Movement: movement, Index: index))
            .GroupBy(x => (x.Movement.ProductId, x.Movement.LocationId))
            .OrderBy(x => x.Key.ProductId)
            .ThenBy(x => x.Key.LocationId, StringComparer.Ordinal);

        var applications = new List<(StockLevel Level, List<(StockMovement Movement, int Index)> Movements)>();
        var shortages = new List<string>();

        foreach (var group in groups)
        {
            var (productId, locationId) = group.Key;
            var delta = group.Sum(x => x.Movement.QuantityDelta);

            var level = await context.StockLevels
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId && x.LocationId == locationId,
                    cancellationToken);

            if (level is null)
            {
                if (group.Any(x => x.Movement.Reason == StockAdjustmentReason.CostRevaluation))
                {
                    context.ChangeTracker.Clear();

                    throw Invalid("locationId", $"Nothing is on hand for product {productId} at {locationId} to revalue.");
                }

                // A missing row counts as zero on hand: only a positive net movement may create it.
                if (delta < 0)
                {
                    shortages.Add($"{productId} at {locationId} (available 0, requested {-delta})");
                    continue;
                }

                level = StockLevel.Create(productId, locationId);
                context.StockLevels.Add(level);
            }
            else if (!level.CanApply(delta))
            {
                shortages.Add($"{productId} at {locationId} (available {level.QuantityOnHand}, requested {-delta})");
                continue;
            }

            // Inbound first, then quantity-neutral, then outbound (stable): the net delta was checked
            // above, so this order never dips below zero mid-group and outbound sees the new average.
            var ordered = group
                .OrderByDescending(x => Math.Sign(x.Movement.QuantityDelta))
                .Select(x => (x.Movement, x.Index))
                .ToList();

            applications.Add((level, ordered));
        }

        if (shortages.Count > 0)
        {
            context.ChangeTracker.Clear();

            throw new InsufficientStockException($"Insufficient stock for product(s): {string.Join("; ", shortages)}.");
        }

        var costs = new (decimal UnitCostBase, decimal ValueDeltaBase)[pending.Count];
        var now = clock.UtcNow;

        // Everything from the first level mutation until the entries are staged is guarded: any throw
        // (pricing, level invariants, ledger-entry guards) must clear the tracker so no half-applied
        // level or entry can leak into a later SaveChanges on the same scoped context.
        try
        {
            foreach (var (level, movements) in applications)
            {
                foreach (var (movement, index) in movements)
                {
                    var cost = Price(level, movement);
                    level.Apply(movement.QuantityDelta, cost.ValueDeltaBase);
                    costs[index] = cost;
                }
            }

            for (var i = 0; i < pending.Count; i++)
            {
                var movement = pending[i];

                context.StockAdjustments.Add(StockAdjustment.Create(
                    movement.ProductId,
                    movement.LocationId,
                    movement.QuantityDelta,
                    movement.Reason,
                    now,
                    movement.PerformedByUserId,
                    movement.Note,
                    movement.SourceType,
                    movement.SourceId,
                    movement.SourceLineId,
                    movement.IdempotencyKey,
                    movement.ReversesAdjustmentId,
                    costs[i].UnitCostBase,
                    costs[i].ValueDeltaBase));
            }
        }
        catch
        {
            context.ChangeTracker.Clear();

            throw;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Values one movement against the level's current state: inbound at its unit cost (or the current
    /// average), outbound at the current average, a reversal at exactly its original's value, a
    /// revaluation as the difference to the re-priced on-hand stock.
    /// </summary>
    private static (decimal UnitCostBase, decimal ValueDeltaBase) Price(
        StockLevel level,
        StockMovement movement)
    {
        if (movement.Reason == StockAdjustmentReason.CostRevaluation)
        {
            if (level.QuantityOnHand == 0)
                throw Invalid("locationId", $"Nothing is on hand for product {level.ProductId} at {level.LocationId} to revalue.");

            var newUnitCost = movement.UnitCostBase
                ?? throw Invalid("unitCost", "A cost revaluation requires a unit cost.");

            var valueChange = level.ValueChangeForRevaluation(newUnitCost);

            // Stock that is on hand must keep some value: a cost so small that it rounds the total to 0
            // would be indistinguishable from an emptied level.
            if (level.TotalValueBase + valueChange == 0m)
                throw Invalid("unitCost", "The unit cost is too small: the revalued stock would be worth nothing.");

            return (newUnitCost, valueChange);
        }

        if (movement.ValueDeltaBaseOverride is { } pinnedValue)
            return (movement.UnitCostBase ?? 0m, pinnedValue);

        if (movement.QuantityDelta > 0)
        {
            if (movement.UnitCostBase is null && level.QuantityOnHand == 0)
                throw Invalid("unitCost", "A unit cost is required when nothing is on hand.");

            var unitCost = movement.UnitCostBase ?? level.AverageCostBase;

            return (unitCost, StockLevel.ValueOfInbound(movement.QuantityDelta, unitCost));
        }

        var quantity = -movement.QuantityDelta;
        var removedValue = level.ValueOfOutbound(quantity);

        return (StockLevel.UnitCostOf(removedValue, quantity), -removedValue);
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private async Task ExecuteWithRetryAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception ex) when (IsRetryable(ex))
        {
            context.ChangeTracker.Clear();

            try
            {
                await operation();
            }
            catch (Exception retryEx) when (IsRetryable(retryEx))
            {
                context.ChangeTracker.Clear();

                throw new ConflictException("Stock was modified concurrently. Please retry.");
            }
        }
    }

    private static bool IsRetryable(Exception ex) =>
        ex is DbUpdateConcurrencyException
        || ex is DbUpdateException dbEx && dbEx.IsUniqueConstraintViolation();
}
