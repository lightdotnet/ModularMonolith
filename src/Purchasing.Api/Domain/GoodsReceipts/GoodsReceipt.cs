using Light.Exceptions;
using StarterKit.Purchasing.Api.Domain.PurchaseOrders;
using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Purchasing.Api.Domain.GoodsReceipts;

/// <summary>
/// One delivery received against a <see cref="PurchaseOrder"/>. Immutable once recorded — there is no
/// edit and no cancel; a mistaken receipt is corrected with a purchase return. It is recorded as
/// <see cref="GoodsReceiptStatus.Posting"/> first (so it has an id to post to Inventory under) and only
/// becomes <see cref="GoodsReceiptStatus.Posted"/> once the inbound stock is confirmed
/// (<see cref="MarkPosted"/>), in the same commit that applies the quantities to the order; a receipt whose
/// posting Inventory refused outright ends as <see cref="GoodsReceiptStatus.Voided"/> (<see cref="Void"/>). Each line
/// freezes the order line's unit cost at receipt time, which is the cost the stock enters Inventory at.
/// The location, supplier and names are snapshots of the order's.
/// </summary>
public class GoodsReceipt : AuditableEntity<long>
{
    private readonly List<GoodsReceiptLine> _lines = [];

    private GoodsReceipt()
    {
    }

    public GoodsReceiptNumber ReceiptNumber { get; private set; } = null!;

    public long PurchaseOrderId { get; private set; }

    public long SupplierId { get; private set; }

    public string SupplierName { get; private set; } = null!;

    public string LocationId { get; private set; } = null!;

    public string LocationName { get; private set; } = null!;

    /// <summary>Supplier delivery note number; unique per purchase order when present.</summary>
    public string? DeliveryNoteRef { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>The user who recorded the delivery; the Inventory posting (including a sweep finishing it) is attributed to them.</summary>
    public string? ReceivedByUserId { get; private set; }

    public GoodsReceiptStatus Status { get; private set; }

    public DateTimeOffset? StockPostedAt { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public string? VoidReason { get; private set; }

    /// <summary>
    /// App-managed optimistic-concurrency token, rotated by <c>PurchasingDbContext</c> whenever the receipt
    /// or a purchase return against it changes (which is what keeps two returns from over-returning a line).
    /// </summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public virtual PurchaseOrder PurchaseOrder { get; private set; } = null!;

    public IReadOnlyList<GoodsReceiptLine> Lines => _lines.AsReadOnly();

    public int TotalQuantity => _lines.Sum(x => x.Quantity);

    public decimal TotalCostBase => _lines.Sum(x => x.LineTotalBase);

    /// <summary>
    /// Records a delivery as a <see cref="GoodsReceiptStatus.Posting"/> receipt. The order decides whether
    /// the delivery is acceptable (<see cref="PurchaseOrder.EnsureCanReceive"/>); the lines then snapshot
    /// the order lines' product data and current unit cost.
    /// </summary>
    public static GoodsReceipt Create(
        PurchaseOrder purchaseOrder,
        string? deliveryNoteRef,
        DateTimeOffset receivedAt,
        string receivedByUserId,
        IReadOnlyCollection<(long PurchaseOrderLineId, int Quantity)> lines,
        IReadOnlyDictionary<long, int> inFlightByLineId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);

        purchaseOrder.EnsureCanReceive(lines, inFlightByLineId);
        purchaseOrder.EnsureReceivedNotBeforeApproval(receivedAt);

        var receipt = new GoodsReceipt
        {
            ReceiptNumber = GoodsReceiptNumber.Generate(now),
            PurchaseOrder = purchaseOrder,
            PurchaseOrderId = purchaseOrder.Id,
            SupplierId = purchaseOrder.SupplierId,
            SupplierName = purchaseOrder.SupplierName,
            LocationId = purchaseOrder.LocationId,
            LocationName = purchaseOrder.LocationName,
            DeliveryNoteRef = string.IsNullOrWhiteSpace(deliveryNoteRef) ? null : deliveryNoteRef.Trim(),
            ReceivedAt = receivedAt,
            ReceivedByUserId = receivedByUserId,
            Status = GoodsReceiptStatus.Posting,
        };

        foreach (var (purchaseOrderLineId, quantity) in lines)
        {
            var orderLine = purchaseOrder.Lines.First(x => x.Id == purchaseOrderLineId);

            receipt._lines.Add(GoodsReceiptLine.Create(
                orderLine.Id,
                orderLine.ProductId,
                orderLine.ProductName,
                orderLine.Sku,
                quantity,
                orderLine.UnitCostAmount));
        }

        return receipt;
    }

    /// <summary>Confirms the inbound stock was posted. A no-op for a receipt that is already posted.</summary>
    public void MarkPosted(DateTimeOffset now)
    {
        if (Status == GoodsReceiptStatus.Posted)
            return;

        Status = GoodsReceiptStatus.Posted;
        StockPostedAt = now;
    }

    /// <summary>
    /// Abandons a receipt whose inbound posting Inventory deterministically refused (and nothing landed),
    /// so it stops counting as pending and no longer blocks closing/cancelling the order. Only a
    /// <see cref="GoodsReceiptStatus.Posting"/> receipt can be voided; the caller is responsible for
    /// having confirmed with Inventory that no posting landed. Nothing is ever reversed in Inventory.
    /// </summary>
    public void Void(
        string reason,
        DateTimeOffset now)
    {
        if (Status != GoodsReceiptStatus.Posting)
            throw new ConflictException("Only a receipt that is still being posted can be voided.");

        Status = GoodsReceiptStatus.Voided;
        VoidReason = reason;
        VoidedAt = now;
    }

    /// <summary>Only a posted receipt has stock that can be returned to the supplier.</summary>
    public void EnsureReturnable()
    {
        if (Status != GoodsReceiptStatus.Posted)
            throw new ConflictException("Only a posted goods receipt can be returned.");
    }

    public GoodsReceiptLine FindLine(long goodsReceiptLineId) =>
        _lines.FirstOrDefault(x => x.Id == goodsReceiptLineId)
        ?? throw new ValidationException(new Dictionary<string, string[]>
        {
            [nameof(goodsReceiptLineId)] = ["Goods receipt line not found on this goods receipt."],
        });

    /// <summary>Rotates the optimistic-concurrency token; called by <c>PurchasingDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");
}

/// <summary>Quantity received for one purchase order line within a <see cref="GoodsReceipt"/>.</summary>
public class GoodsReceiptLine : AuditableEntity<long>
{
    private GoodsReceiptLine()
    {
    }

    public long GoodsReceiptId { get; private set; }

    public long PurchaseOrderLineId { get; private set; }

    public long ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public int Quantity { get; private set; }

    /// <summary>The purchase order line's unit cost at receipt time (base currency) — no override.</summary>
    public decimal UnitCostBase { get; private set; }

    public virtual GoodsReceipt GoodsReceipt { get; private set; } = null!;

    public decimal LineTotalBase => Quantity * UnitCostBase;

    internal static GoodsReceiptLine Create(
        long purchaseOrderLineId,
        long productId,
        string productName,
        string sku,
        int quantity,
        decimal unitCostBase) =>
        new()
        {
            PurchaseOrderLineId = purchaseOrderLineId,
            ProductId = productId,
            ProductName = productName,
            Sku = sku,
            Quantity = quantity,
            UnitCostBase = unitCostBase,
        };
}
