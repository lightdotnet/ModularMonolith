using StarterKit.Purchasing.Contracts.Common;

namespace StarterKit.Purchasing.Contracts.GoodsReceipts;

public class GoodsReceiptDto : BaseDto<long>
{
    public string ReceiptNumber { get; set; } = null!;

    public long PurchaseOrderId { get; set; }

    public string PONumber { get; set; } = null!;

    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string LocationId { get; set; } = null!;

    public string LocationName { get; set; } = null!;

    public string? DeliveryNoteRef { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public GoodsReceiptStatus Status { get; set; }

    public DateTimeOffset? StockPostedAt { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }

    public string? VoidReason { get; set; }

    public int TotalQuantity { get; set; }

    public decimal TotalCostBase { get; set; }

    public IList<GoodsReceiptLineDto> Lines { get; set; } = [];
}

public class GoodsReceiptLineDto : BaseDto<long>
{
    public long PurchaseOrderLineId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitCostBase { get; set; }

    public decimal LineTotalBase { get; set; }
}
