using System.ComponentModel.DataAnnotations;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests;

/// <summary>
/// Configuration for the periodic sweep that reconciles locally <c>Pending</c> leave requests
/// against Approval — the backstop for a missed or failed <c>ApprovalFinalizedIntegrationEvent</c>
/// delivery. Bound from <c>LeaveManagement:Reconciliation</c> and validated on startup — an
/// out-of-range <see cref="IntervalMinutes"/> would otherwise reach <see cref="PeriodicTimer"/>'s
/// constructor in <c>LeaveRequestReconciliationService.ExecuteAsync</c>, which runs once outside
/// the per-tick try/catch and would crash the background service instead of failing fast at
/// startup with a clear configuration error.
/// </summary>
internal sealed class LeaveReconciliationOptions
{
    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int IntervalMinutes { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int BatchSize { get; set; } = 200;
}
