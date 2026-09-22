using System.ComponentModel.DataAnnotations;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders;

/// <summary>
/// Configuration for the periodic sweep that reconciles purchase orders still <c>PendingApproval</c>
/// against Approval — the backstop for a missed or failed <c>ApprovalFinalizedIntegrationEvent</c>
/// delivery. Bound from <c>Purchasing:ApprovalReconciliation</c> and validated on startup so an
/// out-of-range <see cref="IntervalMinutes"/> fails fast instead of crashing the background service when
/// its <see cref="PeriodicTimer"/> is constructed. Every default lives here.
/// </summary>
internal sealed class PurchaseOrderApprovalReconciliationOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 200;
}
