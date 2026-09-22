namespace StarterKit.Purchasing.Contracts.Common;

public enum PurchaseOrderStatus
{
    /// <summary>Editable; not yet sent for approval.</summary>
    Draft = 0,

    PendingApproval = 1,

    /// <summary>Approved and open for goods receipts.</summary>
    Approved = 2,

    /// <summary>Rejected by the approver; editable and can be resubmitted.</summary>
    Rejected = 3,

    PartiallyReceived = 4,

    Received = 5,

    /// <summary>The undelivered remainder of a partially received order was given up.</summary>
    Closed = 6,

    Cancelled = 7,
}
