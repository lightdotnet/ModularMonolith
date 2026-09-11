namespace StarterKit.LeaveManagement.Contracts.LeaveRequests;

public record CreateLeaveRequest(
    LeaveType LeaveType,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string? Reason,
    string ApproverEmployeeId);
