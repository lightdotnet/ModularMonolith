namespace StarterKit.Purchasing.Api.Application.Posting;

/// <summary>
/// Keyset watermarks of the posting reconciliation sweep, carried across ticks by the owning service
/// so no candidate id range is starved. Each is reset to zero when its pass reaches the end of the
/// candidate range.
/// </summary>
internal sealed class PurchasingPostingReconciliationState
{
    public long AfterReceiptId { get; set; }

    public long AfterReturnId { get; set; }
}
