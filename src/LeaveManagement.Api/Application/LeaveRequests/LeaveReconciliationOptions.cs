namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests;

/// <summary>
/// Configuration for the periodic sweep that reconciles locally <c>Pending</c> leave requests
/// against Approval — the backstop for a missed or failed <c>ApprovalFinalizedIntegrationEvent</c>
/// delivery. Bound from <c>LeaveManagement:Reconciliation</c>.
/// </summary>
internal sealed class LeaveReconciliationOptions
{
    public bool Enabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 5;

    public int BatchSize { get; set; } = 200;
}
