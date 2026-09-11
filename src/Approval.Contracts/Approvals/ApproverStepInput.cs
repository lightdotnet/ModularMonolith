namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// One level of an approval chain, already resolved by the calling module (Approval never
/// resolves approvers itself — see <see cref="CreateApprovalRequest"/>). The calling module also
/// supplies <see cref="ApproverName"/>, the display label to show for this approver, since
/// Approval has no view onto the identity/organization data behind the ids.
/// </summary>
public record ApproverStepInput(
    int Level,
    string ApproverUserId,
    string ApproverEmployeeId,
    string? ApproverName = null);
