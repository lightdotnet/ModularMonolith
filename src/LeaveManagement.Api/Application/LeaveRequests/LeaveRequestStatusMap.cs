using StarterKit.Approval.Contracts.Approvals;
using StarterKit.LeaveManagement.Api.Domain.LeaveRequests;

namespace StarterKit.LeaveManagement.Api.Application.LeaveRequests;

/// <summary>
/// Maps an Approval <see cref="ApprovalStatus"/> onto the local <see cref="LeaveRequestStatus"/>.
/// The status is reconciled through the <c>ApprovalFinalizedIntegrationEvent</c> subscriber and a
/// periodic backstop sweep — never on read.
/// </summary>
internal static class LeaveRequestStatusMap
{
    internal const string RequestType = ApprovalRequestTypes.LeaveRequest;

    internal static LeaveRequestStatus MapStatus(ApprovalStatus status) =>
        status switch
        {
            ApprovalStatus.Approved => LeaveRequestStatus.Approved,
            ApprovalStatus.Rejected => LeaveRequestStatus.Rejected,
            ApprovalStatus.Cancelled => LeaveRequestStatus.Cancelled,
            _ => LeaveRequestStatus.Pending,
        };
}
