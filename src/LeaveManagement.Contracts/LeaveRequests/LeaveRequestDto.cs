namespace StarterKit.LeaveManagement.Contracts.LeaveRequests;

public class LeaveRequestDto : BaseDto
{
    public string UserId { get; set; } = null!;

    public string EmployeeId { get; set; } = null!;

    public LeaveType LeaveType { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public string? Reason { get; set; }

    public LeaveRequestStatus Status { get; set; }

    public string? ApprovalRequestId { get; set; }

    public DateTimeOffset Created { get; set; }
}
