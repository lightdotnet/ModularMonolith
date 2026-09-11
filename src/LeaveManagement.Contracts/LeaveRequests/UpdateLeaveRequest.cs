namespace StarterKit.LeaveManagement.Contracts.LeaveRequests;

/// <summary>
/// <see cref="ApproverEmployeeId"/> is only meaningful (and required) on the self-service
/// resubmission path — a <c>.manage</c>-scoped edit is metadata-only and never touches Approval,
/// so it's optional at the contract level.
/// </summary>
public record UpdateLeaveRequest(
    LeaveType LeaveType,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Reason,
    string? ApproverEmployeeId);
