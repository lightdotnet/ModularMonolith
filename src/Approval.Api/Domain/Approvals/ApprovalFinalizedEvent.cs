using StarterKit.Shared.Entities;

namespace StarterKit.Approval.Api.Domain.Approvals;

internal sealed record ApprovalFinalizedEvent(
    string ApprovalRequestId,
    string Title,
    string? DeepLinkUrl,
    string RequesterUserId,
    string DecidedByUserId,
    ApprovalStatus Status) : DomainEvent;
