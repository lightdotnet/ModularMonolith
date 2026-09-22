using StarterKit.Approval.Contracts.Approvals;

namespace StarterKit.Purchasing.Api.Application.PurchaseOrders;

/// <summary>
/// Maps an Approval <see cref="ApprovalStatus"/> onto a purchase order decision. Only a terminal
/// approve/reject decision moves the order; a pending or cancelled workflow leaves it as it is (a
/// requester withdrawal is driven by the withdraw command itself, not learned back from Approval).
/// </summary>
internal static class PurchaseOrderStatusMap
{
    internal const string RequestType = ApprovalRequestTypes.PurchaseOrder;

    internal static bool TryMapDecision(
        ApprovalStatus status,
        out bool approved)
    {
        approved = status == ApprovalStatus.Approved;

        return status is ApprovalStatus.Approved or ApprovalStatus.Rejected;
    }
}
