using StarterKit.Shared.Entities;

namespace StarterKit.Approval.Api.Domain.Approvals;

public class ApprovalStep : AuditableEntity
{
    private ApprovalStep()
    {
    }

    private ApprovalStep(
        int level,
        string approverUserId,
        string approverEmployeeId,
        string? approverName)
    {
        Level = level;
        ApproverUserId = approverUserId;
        ApproverEmployeeId = approverEmployeeId;
        ApproverName = approverName;
        Status = ApprovalStepStatus.Pending;
    }

    public string ApprovalRequestId { get; private set; } = null!;

    public int Level { get; private set; }

    public string ApproverUserId { get; private set; } = null!;

    public string ApproverEmployeeId { get; private set; } = null!;

    /// <summary>
    /// Display label for this approver, captured at creation time by the calling module.
    /// Approval has no view onto identity/organization data, so it cannot resolve this itself.
    /// </summary>
    public string? ApproverName { get; private set; }

    public ApprovalStepStatus Status { get; private set; } = ApprovalStepStatus.Pending;

    public string? Comment { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public ApprovalRequest ApprovalRequest { get; private set; } = null!;

    internal bool IsPending => Status == ApprovalStepStatus.Pending;

    internal static ApprovalStep Create(
        int level,
        string approverUserId,
        string approverEmployeeId,
        string? approverName) =>
        new(
            level,
            approverUserId,
            approverEmployeeId,
            approverName);

    internal void Approve(string? comment, DateTimeOffset decidedAt)
    {
        if (!IsPending)
            return;

        Status = ApprovalStepStatus.Approved;
        Comment = comment;
        DecidedAt = decidedAt;
    }

    internal void Reject(string? comment, DateTimeOffset decidedAt)
    {
        if (!IsPending)
            return;

        Status = ApprovalStepStatus.Rejected;
        Comment = comment;
        DecidedAt = decidedAt;
    }

    internal void Skip()
    {
        if (!IsPending)
            return;

        Status = ApprovalStepStatus.Skipped;
    }
}
