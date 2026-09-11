namespace StarterKit.Approval.Contracts.Approvals;

public class ApprovalStepDto : BaseDto
{
    public string ApprovalRequestId { get; set; } = null!;

    public int Level { get; set; }

    public string ApproverUserId { get; set; } = null!;

    public string ApproverEmployeeId { get; set; } = null!;

    public string? ApproverName { get; set; }

    public ApprovalStepStatus Status { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }
}
