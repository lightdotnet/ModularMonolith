using StarterKit.Inventory.Contracts.Stock;

namespace StarterKit.Inventory.Contracts.Services;

/// <summary>
/// Cross-module seam for stock movements driven by another module (currently Orders). Both calls are
/// idempotent per order line, so a retried or compensating call is safe.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// All-or-nothing stock decrement for a placed order. Throws a conflict if any product would go
    /// below zero, and a validation error if the location does not exist. A line already decremented
    /// for this order is skipped.
    /// </summary>
    Task DecrementForOrderAsync(
        long orderId,
        string locationId,
        IReadOnlyList<StockLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the stock previously decremented for an order by writing a reversal per original
    /// adjustment. No-op when nothing is left to restore.
    /// </summary>
    Task RestoreForOrderAsync(
        long orderId,
        string performedByUserId,
        CancellationToken cancellationToken = default);
}
