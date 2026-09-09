using StarterKit.Shared.Entities;

namespace StarterKit.Approval.Api.Domain.Approvals;

internal sealed record ApprovalRequestCancelledEvent(
    string ApprovalRequestId,
    string Title,
    string? DeepLinkUrl,
    string RequesterUserId,
    string CancelledByUserId,
    string? CurrentApproverUserId) : DomainEvent;
