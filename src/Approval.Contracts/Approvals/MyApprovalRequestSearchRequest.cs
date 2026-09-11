namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// Search input for the self-service "my approvals" endpoint — scoped server-side to the
/// current user via <see cref="Relation"/>, unlike <see cref="ApprovalRequestSearchRequest"/>
/// which is the unrestricted admin search.
/// </summary>
public record MyApprovalRequestSearchRequest : SearchQuery
{
    public ApprovalRelation Relation { get; set; } = ApprovalRelation.All;

    public string? RequestType { get; set; }

    public ApprovalStatus? Status { get; set; }
}
