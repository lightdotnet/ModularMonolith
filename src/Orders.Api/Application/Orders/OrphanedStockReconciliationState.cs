namespace StarterKit.Orders.Api.Application.Orders;

/// <summary>
/// Keyset watermark of the orphaned-stock sweep, carried across ticks by the owning service so no
/// candidate id range is starved. Reset to zero when a pass reaches the end of the candidate range.
/// </summary>
internal sealed class OrphanedStockReconciliationState
{
    public long AfterOrderId { get; set; }
}
