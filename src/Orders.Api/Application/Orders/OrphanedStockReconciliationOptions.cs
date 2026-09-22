using System.ComponentModel.DataAnnotations;

namespace StarterKit.Orders.Api.Application.Orders;

/// <summary>
/// Configuration for the periodic sweep that restores stock still decremented for orders that never
/// reached (or left) the placed state — the backstop for a PlaceOrder whose own save failed after
/// Inventory had already committed the decrement. Bound from <c>Orders:StockReconciliation</c> and
/// validated on startup so an out-of-range <see cref="IntervalMinutes"/> fails fast instead of
/// crashing the background service when its <see cref="PeriodicTimer"/> is constructed.
/// </summary>
internal sealed class OrphanedStockReconciliationOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Minimum age of an Inventory posting before it is considered orphaned, so an in-flight
    /// PlaceOrder is never raced. Must exceed the longest expected PlaceOrder duration.
    /// </summary>
    [Range(2, int.MaxValue)]
    public int MinAgeMinutes { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 200;
}
