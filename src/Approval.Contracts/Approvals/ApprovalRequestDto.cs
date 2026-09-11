namespace StarterKit.Approval.Contracts.Approvals;

public class ApprovalRequestDto : BaseDto
{
    public string RequestType { get; set; } = null!;

    public string RequestId { get; set; } = null!;

    public string RequesterUserId { get; set; } = null!;

    public string? RequesterEmployeeId { get; set; }

    public string? RequesterName { get; set; }

    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public string? DeepLinkUrl { get; set; }

    public string? DocumentTypeId { get; set; }

    public string? DocumentTypeName { get; set; }

    public int CurrentLevel { get; set; }

    public ApprovalStatus Status { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset? FinalizedAt { get; set; }

    public IList<ApprovalStepDto> Steps { get; set; } = [];
}
