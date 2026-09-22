using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseOrders;

public class PurchaseOrderDto : BaseDto<long>
{
    public string PONumber { get; set; } = null!;

    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string LocationId { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public DateTimeOffset? ExpectedAt { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public string? Note { get; set; }

    public string RequesterEmployeeId { get; set; } = null!;

    public string? ApprovalRequestId { get; set; }

    public string? ApproverEmployeeId { get; set; }

    public string? ApproverName { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public DateTimeOffset? ReceivedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string? ClosedReason { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancelledReason { get; set; }

    public DateTimeOffset Created { get; set; }

    public string Currency { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public int TotalOrderedQuantity { get; set; }

    public int TotalReceivedQuantity { get; set; }

    public int TotalOutstandingQuantity { get; set; }

    public IList<PurchaseOrderLineDto> Lines { get; set; } = [];

    /// <summary>Only populated by the get-by-id query.</summary>
    public IList<PurchaseOrderReceiptSummaryDto> Receipts { get; set; } = [];
}

public class PurchaseOrderLineDto : BaseDto<long>
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public int OrderedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    /// <summary>Informational: quantity returned to the supplier after receipt.</summary>
    public int ReturnedQuantity { get; set; }

    public int OutstandingQuantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal LineTotal { get; set; }
}

public class PurchaseOrderReceiptSummaryDto : BaseDto<long>
{
    public string ReceiptNumber { get; set; } = null!;

    public GoodsReceiptStatus Status { get; set; }

    public string? DeliveryNoteRef { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public int TotalQuantity { get; set; }
}
