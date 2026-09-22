using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.PurchaseReturns;

public class PurchaseReturnDto : BaseDto<long>
{
    public string ReturnNumber { get; set; } = null!;

    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public long GoodsReceiptId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public long PurchaseOrderId { get; set; }

    public string LocationId { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public PurchaseReturnReason Reason { get; set; }

    public string? Note { get; set; }

    public PurchaseReturnStatus Status { get; set; }

    public DateTimeOffset? PostedAt { get; set; }

    public DateTimeOffset Created { get; set; }

    public int TotalQuantity { get; set; }

    /// <summary>Quantity times the receipt unit cost, frozen when the return is posted.</summary>
    public decimal? ExpectedCreditBase { get; set; }

    /// <summary>Cost Inventory actually removed from stock at its moving average; null until posted.</summary>
    public decimal? CostRemovedBase { get; set; }

    public string? CreditNoteNumber { get; set; }

    public decimal? CreditAmountBase { get; set; }

    public DateTimeOffset? CreditedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancelledReason { get; set; }

    public IList<PurchaseReturnLineDto> Lines { get; set; } = [];
}

public class PurchaseReturnLineDto : BaseDto<long>
{
    public long GoodsReceiptLineId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal ReceiptUnitCostBase { get; set; }

    public decimal? CostRemovedBase { get; set; }

    public PurchaseReturnReason? Reason { get; set; }
}
