namespace StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

public class LeaveRequestByIdSpec : Specification<LeaveRequest>
{
    public LeaveRequestByIdSpec(string leaveRequestId)
    {
        Where(x => x.Id == leaveRequestId);
    }
}
