using System.ComponentModel.DataAnnotations;

namespace StarterKit.Purchasing.Api.Application.Posting;

/// <summary>
/// Configuration for the periodic sweep that finishes goods receipts and purchase returns whose
/// Inventory posting was started but never confirmed — the backstop for a request whose process died (or
/// whose second commit failed) between the Inventory call and the Purchasing save. Bound from
/// <c>Purchasing:PostingReconciliation</c> and validated on startup so an out-of-range
/// <see cref="IntervalMinutes"/> fails fast instead of crashing the background service when its
/// <see cref="PeriodicTimer"/> is constructed. Every default lives here; nothing is required in
/// configuration.
/// </summary>
internal sealed class PurchasingPostingReconciliationOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Minimum time a receipt/return must have been in <c>Posting</c> before it is picked up, so an
    /// in-flight request is never raced. Must exceed the longest expected request duration.
    /// </summary>
    [Range(2, int.MaxValue)]
    public int StuckAfterMinutes { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 200;
}
