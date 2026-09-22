using StarterKit.Inventory.Contracts.Common;
using StarterKit.Inventory.Contracts.Stock;

namespace StarterKit.Inventory.Contracts.Services;

/// <summary>
/// Cross-module seam for stock movements driven by another module. Every posting call is idempotent
/// per source line, so a retried or compensating call is safe. All costs are in base currency.
/// <para>
/// A single call posts either only inbound or only outbound lines, and the seam does not support
/// mixing inbound and outbound movements for the same product/location in one batch: only the net
/// delta is availability-checked. A cost revaluation on a level that does not exist yet is rejected
/// even if an inbound in the same batch would create stock.
/// </para>
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// All-or-nothing stock decrement for a placed order, valued at the current moving average. Throws a
    /// conflict if any product would go below zero, and a validation error if the location does not
    /// exist. A line already decremented for this order is skipped; the returned results always cover
    /// every requested line (recomputed from the stored adjustments on a replay).
    /// </summary>
    Task<IReadOnlyList<StockPostingResult>> DecrementForOrderAsync(
        long orderId,
        string locationId,
        IReadOnlyList<StockLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the stock previously decremented for an order by writing a reversal per original
    /// adjustment. No-op when nothing is left to restore. When <paramref name="postedBefore"/> is
    /// supplied, only adjustments written at or before it are reversed, so a placement committed after
    /// that instant (e.g. by a concurrent PlaceOrder) is never undone.
    /// </summary>
    Task RestoreForOrderAsync(
        long orderId,
        string performedByUserId,
        CancellationToken cancellationToken = default,
        DateTimeOffset? postedBefore = null);

    /// <summary>
    /// The subset of <paramref name="candidateSourceIds"/> (of the given source type) that still hold at
    /// least one posted, unreversed adjustment written at or before <paramref name="postedBefore"/>,
    /// ordered by source id ascending. Returns empty for empty input without querying.
    /// </summary>
    Task<IReadOnlyList<long>> FilterSourceIdsWithUnreversedPostingsAsync(
        StockSourceType sourceType,
        IReadOnlyCollection<long> candidateSourceIds,
        DateTimeOffset postedBefore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives stock at the given unit costs. Accepts <see cref="StockSourceType.GoodsReceipt"/> and
    /// <see cref="StockSourceType.TransferReceipt"/> only. A repeated call is a no-op that returns the
    /// results already posted.
    /// </summary>
    Task<IReadOnlyList<StockPostingResult>> ReceiveStockAsync(
        StockSourceType sourceType,
        long sourceId,
        string locationId,
        IReadOnlyList<StockInLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// All-or-nothing stock issue at the current moving average, with strict no-oversell. Accepts
    /// <see cref="StockSourceType.Transfer"/> and <see cref="StockSourceType.PurchaseReturn"/> only. A
    /// repeated call is a no-op that returns the results already posted.
    /// </summary>
    Task<IReadOnlyList<StockPostingResult>> IssueStockAsync(
        StockSourceType sourceType,
        long sourceId,
        string locationId,
        IReadOnlyList<StockOutLine> lines,
        string performedByUserId,
        CancellationToken cancellationToken = default);
}
