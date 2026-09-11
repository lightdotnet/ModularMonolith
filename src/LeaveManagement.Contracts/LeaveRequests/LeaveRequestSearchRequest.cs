namespace StarterKit.LeaveManagement.Contracts.LeaveRequests;

public record LeaveRequestSearchRequest : SearchQuery
{
    public LeaveType? LeaveType { get; set; }

    public LeaveRequestStatus? Status { get; set; }

    /// <summary>
    /// Only honored for callers with <c>leave.requests.manage</c> — everyone else is always
    /// scoped to their own employee id regardless of what's supplied here.
    /// </summary>
    public string? EmployeeId { get; set; }
}
