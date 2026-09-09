namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// Lean read-model of an approval request's current state, for the owning module to reconcile
/// its own copy of the status (e.g. to block edits once approved) without pulling the full
/// <see cref="ApprovalRequestDto"/> and its steps.
/// </summary>
public sealed record ApprovalStatusView(
    string ApprovalRequestId,
    string RequestType,
    string RequestId,
    ApprovalStatus Status,
    int CurrentLevel);
