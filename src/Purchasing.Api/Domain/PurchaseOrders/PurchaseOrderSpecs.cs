namespace StarterKit.Purchasing.Api.Domain.PurchaseOrders;

public class PurchaseOrderByIdSpec : Specification<PurchaseOrder>
{
    public PurchaseOrderByIdSpec(long purchaseOrderId)
    {
        Where(x => x.Id == purchaseOrderId);
    }
}

/// <summary>
/// Orders still <see cref="PurchaseOrderStatus.PendingApproval"/> that carry an approval workflow — the
/// set the periodic sweep reconciles against Approval, as a backstop for a missed
/// <c>ApprovalFinalizedIntegrationEvent</c>.
/// </summary>
public class ReconcilablePurchaseOrdersSpec : Specification<PurchaseOrder>
{
    public ReconcilablePurchaseOrdersSpec()
    {
        Where(x => x.Status == PurchaseOrderStatus.PendingApproval && x.ApprovalRequestId != null);
    }
}
