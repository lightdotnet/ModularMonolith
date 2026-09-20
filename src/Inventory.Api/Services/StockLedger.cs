using Light.Exceptions;
using StarterKit.Inventory.Api.Data;
using StarterKit.Inventory.Api.Domain.StockAdjustments;
using StarterKit.Inventory.Api.Domain.StockLevels;
using StarterKit.Persistence.Extensions;
using StarterKit.Shared;

namespace StarterKit.Inventory.Api.Services;

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
        CancellationToken cancellationToken) =>
        ExecuteWithRetryAsync(() => ReverseOnceAsync(orderId, performedByUserId, cancellationToken));

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
        CancellationToken cancellationToken)
    {
        var originals = await context.StockAdjustments
            .Where(x => x.SourceOrderId == orderId && x.Reason == StockAdjustmentReason.OrderPlacement)
            .ToListAsync(cancellationToken);

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
                SourceOrderId: orderId,
                SourceOrderLineId: original.SourceOrderLineId,
                IdempotencyKey: $"order-restore:{orderId}:{original.SourceOrderLineId}:{original.Id}",
                ReversesAdjustmentId: original.Id));
        }

        await ApplyPendingAsync(movements, cancellationToken);
    }

    private async Task ApplyPendingAsync(
        IReadOnlyList<StockMovement> pending,
        CancellationToken cancellationToken)
    {
        var groups = pending
            .GroupBy(x => (x.ProductId, x.LocationId))
            .OrderBy(x => x.Key.ProductId)
            .ThenBy(x => x.Key.LocationId, StringComparer.Ordinal);

        var applications = new List<(StockLevel Level, int Delta)>();
        var shortages = new List<string>();

        foreach (var group in groups)
        {
            var (productId, locationId) = group.Key;
            var delta = group.Sum(x => x.QuantityDelta);

            var level = await context.StockLevels
                .FirstOrDefaultAsync(
                    x => x.ProductId == productId && x.LocationId == locationId,
                    cancellationToken);

            if (level is null)
            {
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

            applications.Add((level, delta));
        }

        if (shortages.Count > 0)
        {
            context.ChangeTracker.Clear();

            throw new ConflictException($"Insufficient stock for product(s): {string.Join("; ", shortages)}.");
        }

        foreach (var (level, delta) in applications)
            level.Apply(delta);

        var now = clock.UtcNow;

        foreach (var movement in pending)
        {
            context.StockAdjustments.Add(StockAdjustment.Create(
                movement.ProductId,
                movement.LocationId,
                movement.QuantityDelta,
                movement.Reason,
                now,
                movement.PerformedByUserId,
                movement.Note,
                movement.SourceOrderId,
                movement.SourceOrderLineId,
                movement.IdempotencyKey,
                movement.ReversesAdjustmentId));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

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
