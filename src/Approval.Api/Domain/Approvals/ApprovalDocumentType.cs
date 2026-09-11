using StarterKit.Shared.Entities;

namespace StarterKit.Approval.Api.Domain.Approvals;

public class ApprovalDocumentType : AuditableEntity
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
