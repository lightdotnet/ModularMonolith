namespace StarterKit.Approval.Api.Domain.Approvals;

public class ApprovalRequestByIdSpec : Specification<ApprovalRequest>
{
    public ApprovalRequestByIdSpec(string requestId)
    {
        Where(x => x.Id == requestId);
    }
}
