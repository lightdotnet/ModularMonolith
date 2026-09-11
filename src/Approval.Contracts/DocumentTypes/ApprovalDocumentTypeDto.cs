namespace StarterKit.Approval.Contracts.DocumentTypes;

public class ApprovalDocumentTypeDto : BaseDto
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset Created { get; set; }
}
