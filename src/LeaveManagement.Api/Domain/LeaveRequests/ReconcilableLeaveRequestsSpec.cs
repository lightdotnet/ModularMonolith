namespace StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

/// <summary>
/// Locally-<see cref="LeaveRequestStatus.Pending"/> rows that carry an approval workflow — the set
/// the integration-event subscriber and the periodic sweep reconcile against Approval.
/// </summary>
public class ReconcilableLeaveRequestsSpec : Specification<LeaveRequest>
{
    public ReconcilableLeaveRequestsSpec()
    {
        Where(x => x.Status == LeaveRequestStatus.Pending && x.ApprovalRequestId != null);
    }
}
