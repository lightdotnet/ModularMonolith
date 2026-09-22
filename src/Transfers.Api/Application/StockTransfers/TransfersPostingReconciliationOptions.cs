using System.ComponentModel.DataAnnotations;

namespace StarterKit.Transfers.Api.Application.StockTransfers;

/// <summary>
/// Configuration for the periodic sweep that finishes transfers/receipts whose Inventory posting was
/// started but never confirmed — the backstop for a dispatch or receive whose process died (or whose
/// second commit failed) between the Inventory call and the Transfers save. Bound from
/// <c>Transfers:PostingReconciliation</c> and validated on startup so an out-of-range
/// <see cref="IntervalMinutes"/> fails fast instead of crashing the background service when its
/// <see cref="PeriodicTimer"/> is constructed. Every default lives here; nothing is required in
/// configuration.
/// </summary>
internal sealed class TransfersPostingReconciliationOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Minimum time a transfer/receipt must have been in <c>Posting</c> before it is picked up, so an
    /// in-flight dispatch/receive is never raced. Must exceed the longest expected request duration.
    /// </summary>
    [Range(2, int.MaxValue)]
    public int StuckAfterMinutes { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 200;
}
