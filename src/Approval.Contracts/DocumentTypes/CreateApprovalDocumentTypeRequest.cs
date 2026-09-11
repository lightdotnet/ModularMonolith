namespace StarterKit.Approval.Contracts.DocumentTypes;

public record CreateApprovalDocumentTypeRequest(
    string Name,
    string Code,
    string? Description,
    bool IsActive);
