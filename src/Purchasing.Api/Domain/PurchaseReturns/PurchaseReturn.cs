using Light.Exceptions;
using StarterKit.Purchasing.Api.Domain.GoodsReceipts;
using StarterKit.Shared.Entities;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Purchasing.Api.Domain.PurchaseReturns;

/// <summary>
/// Goods sent back to a supplier against one <see cref="GoodsReceipt"/>. Every state change and quantity
/// rule lives here:
/// <list type="bullet">
/// <item><description><c>Draft</c> is the only editable state (<see cref="UpdateDraft"/>) and the only cancellable one (<see cref="Cancel"/>).</description></item>
/// <item><description><c>Draft</c> → <c>Posting</c> (<see cref="BeginPost"/>) → <c>Posted</c> (<see cref="CompletePost"/>), or back to <c>Draft</c> if Inventory refuses the issue (<see cref="AbortPost"/>).</description></item>
/// <item><description><c>Posted</c> → <c>Credited</c> (<see cref="MarkCredited"/>) records the supplier's credit note; purely informational bookkeeping that gates nothing.</description></item>
/// </list>
/// Over-return is prevented by the quantity rule: across all non-cancelled returns of a receipt line, the
/// returned quantity cannot exceed what that line received. The caller passes in what other returns
/// already claim. There is no approval and no supplier-confirmation gate. A posted return is never
/// reversed. The location is fixed to the receipt's, and the supplier/order are snapshots of it.
/// </summary>
public class PurchaseReturn : AuditableEntity<long>
{
    private readonly List<PurchaseReturnLine> _lines = [];

    private PurchaseReturn()
    {
    }

    public PurchaseReturnNumber ReturnNumber { get; private set; } = null!;

    public long SupplierId { get; private set; }

    public string SupplierName { get; private set; } = null!;

    public long GoodsReceiptId { get; private set; }

    public long PurchaseOrderId { get; private set; }

    public string LocationId { get; private set; } = null!;

    public string LocationName { get; private set; } = null!;

    public PurchaseReturnReason Reason { get; private set; }

    public string? Note { get; private set; }

    public PurchaseReturnStatus Status { get; private set; }

    /// <summary>When the return entered <see cref="PurchaseReturnStatus.Posting"/>; drives the reconciliation grace period.</summary>
    public DateTimeOffset? PostingStartedAt { get; private set; }

    public DateTimeOffset? PostedAt { get; private set; }

    /// <summary>The user who posted the return; the Inventory issue (including a sweep finishing it) is attributed to them.</summary>
    public string? PostedBy { get; private set; }

    /// <summary>Quantity times the receipt unit cost, frozen at post; the variance against the cost removed is informational.</summary>
    public decimal? ExpectedCreditBase { get; private set; }

    public string? CreditNoteNumber { get; private set; }

    public decimal? CreditAmountBase { get; private set; }

    public DateTimeOffset? CreditedAt { get; private set; }

    public string? CreditedBy { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancelledBy { get; private set; }

    public string? CancelledReason { get; private set; }

    /// <summary>App-managed optimistic-concurrency token, rotated by <c>PurchasingDbContext</c> whenever the return or its lines change.</summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public virtual GoodsReceipt GoodsReceipt { get; private set; } = null!;

    public IReadOnlyList<PurchaseReturnLine> Lines => _lines.AsReadOnly();

    public int TotalQuantity => _lines.Sum(x => x.Quantity);

    /// <summary>Cost Inventory removed from stock at its moving average; null until posted.</summary>
    public decimal? CostRemovedBase =>
        Status is PurchaseReturnStatus.Posted or PurchaseReturnStatus.Credited
            ? _lines.Sum(x => x.CostRemovedBase ?? 0m)
            : null;

    /// <param name="alreadyReturnedByReceiptLineId">
    /// Quantity already claimed per receipt line by the receipt's other non-cancelled returns.
    /// </param>
    public static PurchaseReturn Create(
        GoodsReceipt receipt,
        PurchaseReturnReason reason,
        string? note,
        IReadOnlyCollection<(long GoodsReceiptLineId, int Quantity, PurchaseReturnReason? Reason)> lines,
        IReadOnlyDictionary<long, int> alreadyReturnedByReceiptLineId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        receipt.EnsureReturnable();

        var purchaseReturn = new PurchaseReturn
        {
            ReturnNumber = PurchaseReturnNumber.Generate(now),
            SupplierId = receipt.SupplierId,
            SupplierName = receipt.SupplierName,
            GoodsReceipt = receipt,
            GoodsReceiptId = receipt.Id,
            PurchaseOrderId = receipt.PurchaseOrderId,
            LocationId = receipt.LocationId,
            LocationName = receipt.LocationName,
            Status = PurchaseReturnStatus.Draft,
        };

        purchaseReturn.Reason = reason;
        purchaseReturn.Note = note;
        purchaseReturn.ReplaceLines(receipt, lines, alreadyReturnedByReceiptLineId);

        return purchaseReturn;
    }

    /// <summary>Replaces the reason, note and lines of a draft return, under the same quantity rule as creation.</summary>
    public void UpdateDraft(
        GoodsReceipt receipt,
        PurchaseReturnReason reason,
        string? note,
        IReadOnlyCollection<(long GoodsReceiptLineId, int Quantity, PurchaseReturnReason? Reason)> lines,
        IReadOnlyDictionary<long, int> alreadyReturnedByReceiptLineId)
    {
        EnsureDraft();

        if (receipt.Id != GoodsReceiptId)
            throw Invalid(nameof(receipt), "The goods receipt does not belong to this return.");

        Reason = reason;
        Note = note;

        ReplaceLines(receipt, lines, alreadyReturnedByReceiptLineId);
    }

    /// <summary>Fences the return as <c>Posting</c> so it can be persisted before its stock issue is posted to Inventory.</summary>
    public void BeginPost(
        string postedByUserId,
        DateTimeOffset now)
    {
        EnsureDraft("Only a draft return can be posted.");

        if (_lines.Count == 0)
            throw Invalid("lines", "At least one return line is required.");

        Status = PurchaseReturnStatus.Posting;
        PostingStartedAt = now;
        PostedBy = postedByUserId;
    }

    /// <summary>
    /// Records the outcome of the Inventory stock issue: the value removed per line
    /// (<paramref name="costRemovedByLineId"/>, keyed by return line id) and the frozen expected credit.
    /// </summary>
    public void CompletePost(
        IReadOnlyDictionary<long, decimal> costRemovedByLineId,
        DateTimeOffset now)
    {
        if (Status != PurchaseReturnStatus.Posting)
            throw new ConflictException("Only a return whose stock issue is being posted can be completed.");

        foreach (var line in _lines)
        {
            if (!costRemovedByLineId.TryGetValue(line.Id, out var costRemovedBase))
                throw Invalid("lines", $"No posted cost was returned for return line {line.Id}.");

            line.MarkPosted(costRemovedBase);
        }

        ExpectedCreditBase = _lines.Sum(x => x.Quantity * x.ReceiptUnitCostBase);
        Status = PurchaseReturnStatus.Posted;
        PostedAt = now;
        PostingStartedAt = null;
    }

    /// <summary>Returns a return whose stock issue was refused (for example insufficient stock) to <c>Draft</c>.</summary>
    public void AbortPost()
    {
        if (Status != PurchaseReturnStatus.Posting)
            throw new ConflictException("Only a return whose stock issue is being posted can be aborted.");

        Status = PurchaseReturnStatus.Draft;
        PostingStartedAt = null;
    }

    /// <summary>Records the supplier's credit note. Bookkeeping only — it changes no stock and gates nothing.</summary>
    public void MarkCredited(
        string creditNoteNumber,
        decimal creditAmountBase,
        string creditedByUserId,
        DateTimeOffset now)
    {
        if (Status != PurchaseReturnStatus.Posted)
            throw new ConflictException("Only a posted return that has not been credited yet can be marked credited.");

        Status = PurchaseReturnStatus.Credited;
        CreditNoteNumber = creditNoteNumber;
        CreditAmountBase = creditAmountBase;
        CreditedAt = now;
        CreditedBy = creditedByUserId;
    }

    /// <summary>Only a draft can be cancelled; a posted return is never reversed.</summary>
    public void Cancel(
        string reason,
        string cancelledByUserId,
        DateTimeOffset now)
    {
        EnsureDraft("Only a draft return can be cancelled.");

        Status = PurchaseReturnStatus.Cancelled;
        CancelledAt = now;
        CancelledReason = reason;
        CancelledBy = cancelledByUserId;
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>PurchasingDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    /// <summary>Called by the create handler's bounded retry loop after a generated number collides.</summary>
    internal void RegenerateReturnNumber(DateTimeOffset now) => ReturnNumber = PurchaseReturnNumber.Generate(now);

    private void ReplaceLines(
        GoodsReceipt receipt,
        IReadOnlyCollection<(long GoodsReceiptLineId, int Quantity, PurchaseReturnReason? Reason)> lines,
        IReadOnlyDictionary<long, int> alreadyReturnedByReceiptLineId)
    {
        if (lines.Count == 0)
            throw Invalid("lines", "At least one return line is required.");

        if (lines.Select(x => x.GoodsReceiptLineId).Distinct().Count() != lines.Count)
            throw Invalid("lines", "A receipt line can appear only once per return.");

        var newLines = new List<PurchaseReturnLine>();

        foreach (var (goodsReceiptLineId, quantity, lineReason) in lines)
        {
            var receiptLine = receipt.FindLine(goodsReceiptLineId);

            if (quantity <= 0)
                throw Invalid("lines", "Every returned quantity must be greater than zero.");

            var available = receiptLine.Quantity - alreadyReturnedByReceiptLineId.GetValueOrDefault(receiptLine.Id);

            if (quantity > available)
                throw Invalid(
                    "lines",
                    $"Cannot return {quantity} of '{receiptLine.ProductName}': only {Math.Max(available, 0)} of the received quantity can still be returned.");

            newLines.Add(PurchaseReturnLine.Create(
                receiptLine.Id,
                receiptLine.PurchaseOrderLineId,
                receiptLine.ProductId,
                receiptLine.ProductName,
                receiptLine.Sku,
                quantity,
                receiptLine.UnitCostBase,
                lineReason));
        }

        _lines.Clear();
        _lines.AddRange(newLines);
    }

    private void EnsureDraft(string message = "This return is no longer a draft and cannot be modified.")
    {
        if (Status != PurchaseReturnStatus.Draft)
            throw new ConflictException(message);
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

/// <summary>
/// A quantity of one goods receipt line sent back. <see cref="ReceiptUnitCostBase"/> is a snapshot of the
/// receipt line's cost; <see cref="CostRemovedBase"/> is what Inventory actually removed (its moving
/// average at post time), set once when the return is posted.
/// </summary>
public class PurchaseReturnLine : AuditableEntity<long>
{
    private PurchaseReturnLine()
    {
    }

    public long PurchaseReturnId { get; private set; }

    public long GoodsReceiptLineId { get; private set; }

    /// <summary>The purchase order line the received goods belong to; lets posting update the order without loading the receipt.</summary>
    public long PurchaseOrderLineId { get; private set; }

    public long ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public string Sku { get; private set; } = null!;

    public int Quantity { get; private set; }

    public decimal ReceiptUnitCostBase { get; private set; }

    public decimal? CostRemovedBase { get; private set; }

    public PurchaseReturnReason? Reason { get; private set; }

    public virtual PurchaseReturn PurchaseReturn { get; private set; } = null!;

    internal static PurchaseReturnLine Create(
        long goodsReceiptLineId,
        long purchaseOrderLineId,
        long productId,
        string productName,
        string sku,
        int quantity,
        decimal receiptUnitCostBase,
        PurchaseReturnReason? reason) =>
        new()
        {
            GoodsReceiptLineId = goodsReceiptLineId,
            PurchaseOrderLineId = purchaseOrderLineId,
            ProductId = productId,
            ProductName = productName,
            Sku = sku,
            Quantity = quantity,
            ReceiptUnitCostBase = receiptUnitCostBase,
            Reason = reason,
        };

    internal void MarkPosted(decimal costRemovedBase) => CostRemovedBase = costRemovedBase;
}
