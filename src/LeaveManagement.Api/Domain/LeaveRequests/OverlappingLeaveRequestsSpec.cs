namespace StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

/// <summary>
/// Leave requests for the same employee that still block a new request for an overlapping period:
/// status <see cref="LeaveRequestStatus.Pending"/> or <see cref="LeaveRequestStatus.Approved"/>,
/// with a stored date range that inclusively overlaps the given period.
/// </summary>
public class OverlappingLeaveRequestsSpec : Specification<LeaveRequest>
{
    /// <param name="employeeId">The employee whose existing requests are checked.</param>
    /// <param name="period">The candidate period to check for an overlap.</param>
    /// <param name="excludeRequestId">Pass to ignore the row currently being edited.</param>
    public OverlappingLeaveRequestsSpec(
        string employeeId,
        DateRange period,
        string? excludeRequestId = null)
    {
        Where(x =>
            x.EmployeeId == employeeId
            && (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved)
            && period.Start <= x.Period.End
            && x.Period.Start <= period.End
            && (excludeRequestId == null || x.Id != excludeRequestId));
    }
}
