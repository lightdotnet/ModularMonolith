namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// Input for <see cref="Services.IApprovalService.CreateAsync"/>. The calling module is
/// responsible for resolving <see cref="ApproverChain"/> (Approval has no knowledge of
/// org structure, employee levels, etc.) so this same shape works for any future request type.
/// <see cref="DocumentTypeId"/> is an optional tag referencing an admin-managed
/// <c>ApprovalDocumentType</c>; pass <c>null</c> when the caller has no document type.
/// <see cref="RequesterEmployeeId"/> is optional opaque bookkeeping — <c>null</c> when the
/// requester's account is not linked to an employee. <see cref="RequesterName"/> is the display
/// label to show for the requester, resolved by the caller (Approval cannot resolve it itself).
/// Prefer named arguments: parameters are grouped, not append-only.
/// </summary>
public record CreateApprovalRequest(
    string RequestType,
    string RequestId,
    string RequesterUserId,
    string? RequesterEmployeeId,
    string? RequesterName,
    string Title,
    string? Content,
    string? DeepLinkUrl,
    string? DocumentTypeId,
    IReadOnlyList<ApproverStepInput> ApproverChain);
