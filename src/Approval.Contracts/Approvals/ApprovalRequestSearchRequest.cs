namespace StarterKit.Approval.Contracts.Approvals;

public record ApprovalRequestSearchRequest : SearchQuery
{
    public string? RequestType { get; set; }

    public ApprovalStatus? Status { get; set; }
}
