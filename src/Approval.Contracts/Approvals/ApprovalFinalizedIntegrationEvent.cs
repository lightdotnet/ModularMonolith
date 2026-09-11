namespace StarterKit.Approval.Contracts.Approvals;

/// <summary>
/// In-process cross-module notification raised by Approval immediately after an approval
/// request reaches a terminal <see cref="ApprovalStatus.Approved"/> or
/// <see cref="ApprovalStatus.Rejected"/> outcome and the change is committed. A
/// requester-initiated cancellation does <b>not</b> raise this event — the initiating module
/// drives the cancellation itself and learns that outcome directly from
/// <c>IApprovalService.CancelAsync</c>'s result. The owning module (identified by
/// <see cref="RequestType"/>) subscribes with an <c>INotificationHandler&lt;T&gt;</c> to
/// reconcile its own copy of the status. Delivery is best-effort-immediate; a periodic backstop
/// in the owning module covers a missed or failed in-process delivery.
/// <see cref="ApprovalRequestId"/> lets a subscriber ignore a late event for a superseded
/// workflow.
/// </summary>
public sealed record ApprovalFinalizedIntegrationEvent(
    string RequestType,
    string RequestId,
    string ApprovalRequestId,
    ApprovalStatus Status) : INotification;
