using Light.Exceptions;
using StarterKit.Purchasing.Api.Domain.Suppliers;
using StarterKit.Shared.Constants;
using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Purchasing.Api.Domain.PurchaseOrders;

/// <summary>
/// Aggregate root for buying goods from a supplier into one receiving location. Every state change and
/// quantity rule lives here:
/// <list type="bullet">
/// <item><description><c>Draft</c>/<c>Rejected</c> are the only editable states (header and lines); <see cref="Submit"/> moves either to <c>PendingApproval</c>, <see cref="Withdraw"/> moves it back to <c>Draft</c>.</description></item>
/// <item><description><see cref="ApplyApprovalOutcome"/> is the sole authority on turning an approval decision into <c>Approved</c>/<c>Rejected</c>, and ignores any decision that belongs to a superseded workflow.</description></item>
/// <item><description><c>Approved</c>/<c>PartiallyReceived</c> → <c>PartiallyReceived</c>/<c>Received</c> through <see cref="ApplyReceipt"/>; <c>PartiallyReceived</c> → <c>Closed</c> through <see cref="Close"/>.</description></item>
/// <item><description><see cref="Cancel"/> works from <c>Draft</c>, <c>Rejected</c>, or <c>Approved</c> while nothing has been received.</description></item>
/// </list>
/// The supplier is a real same-module reference; the location is an opaque cross-module id. Names are
/// snapshots taken by the caller. Approval itself is driven by the command handlers through Approval's
/// <c>IApprovalService</c> — the aggregate only tracks the resulting workflow id and status. Stock is
/// posted to Inventory by the goods-receipt flow; the aggregate only tracks the resulting quantities.
/// </summary>
public class PurchaseOrder : AuditableEntity<long>
{
    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
    }

    public PurchaseOrderNumber PONumber { get; private set; } = null!;

    public long SupplierId { get; private set; }

    public string SupplierName { get; private set; } = null!;

    /// <summary>The receiving location (opaque Location id).</summary>
    public string LocationId { get; private set; } = null!;

    public string LocationName { get; private set; } = null!;

    public DateTimeOffset? ExpectedAt { get; private set; }

    public string RequesterUserId { get; private set; } = null!;

    public string RequesterEmployeeId { get; private set; } = null!;

    public string? ApproverEmployeeId { get; private set; }

    public string? ApproverName { get; private set; }

    /// <summary>The id of the current Approval workflow; replaced by a fresh one on every (re)submission.</summary>
    public string? ApprovalRequestId { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public DateTimeOffset? ReceivedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public string? ClosedBy { get; private set; }

    public string? ClosedReason { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancelledBy { get; private set; }

    public string? CancelledReason { get; private set; }

    /// <summary>
    /// App-managed optimistic-concurrency token, rotated by <c>PurchasingDbContext</c> whenever the order,
    /// its lines, or a goods receipt against it changes.
    /// </summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public virtual Supplier Supplier { get; private set; } = null!;

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public Money TotalAmount => new(SaturatingSum(_lines.Select(x => x.LineTotal.Amount)), CurrencyConstants.Default);

    public int TotalOrderedQuantity => _lines.Sum(x => x.OrderedQuantity);

    public int TotalReceivedQuantity => _lines.Sum(x => x.ReceivedQuantity);

    public int TotalOutstandingQuantity => _lines.Sum(x => x.OutstandingQuantity);

    public static PurchaseOrder Create(
        long supplierId,
        string supplierName,
        string locationId,
        string locationName,
        DateTimeOffset? expectedAt,
        string requesterUserId,
        string requesterEmployeeId,
        string? note,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(requesterUserId))
            throw Invalid(nameof(requesterUserId), "A requester user is required.");

        if (string.IsNullOrWhiteSpace(requesterEmployeeId))
            throw Invalid(nameof(requesterEmployeeId), "A requester employee is required.");

        var order = new PurchaseOrder
        {
            Status = PurchaseOrderStatus.Draft,
            PONumber = PurchaseOrderNumber.Generate(now),
            RequesterUserId = requesterUserId,
            RequesterEmployeeId = requesterEmployeeId,
        };

        order.ApplyHeader(
            supplierId,
            supplierName,
            locationId,
            locationName,
            expectedAt,
            note);

        return order;
    }

    public void UpdateHeader(
        long supplierId,
        string supplierName,
        string locationId,
        string locationName,
        DateTimeOffset? expectedAt,
        string? note)
    {
        EnsureEditable();

        ApplyHeader(
            supplierId,
            supplierName,
            locationId,
            locationName,
            expectedAt,
            note);
    }

    public void AddLine(
        long productId,
        string productName,
        string sku,
        int quantity,
        Money unitCost)
    {
        EnsureEditable();

        if (_lines.Any(x => x.ProductId == productId))
            throw Invalid(nameof(productId), "This product is already on the purchase order.");

        _lines.Add(PurchaseOrderLine.Create(
            Id,
            productId,
            productName,
            sku,
            quantity,
            unitCost));
    }

    public void UpdateLine(
        long purchaseOrderLineId,
        int quantity,
        Money unitCost)
    {
        EnsureEditable();

        FindLine(purchaseOrderLineId).Update(quantity, unitCost);
    }

    public void RemoveLine(long purchaseOrderLineId)
    {
        EnsureEditable();

        _lines.Remove(FindLine(purchaseOrderLineId));
    }

    /// <summary>
    /// The guard part of <see cref="Submit"/>, callable before the Approval workflow is created so a
    /// submission that cannot succeed never leaves a workflow behind.
    /// </summary>
    public void EnsureCanSubmit(string requesterUserId)
    {
        EnsureRequester(requesterUserId, "submit");

        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Rejected))
            throw new ConflictException("Only a draft or rejected purchase order can be submitted for approval.");

        if (_lines.Count == 0)
            throw Invalid("lines", "At least one purchase order line is required.");
    }

    /// <summary>
    /// Moves a draft or rejected order to <c>PendingApproval</c> against the freshly created
    /// <paramref name="approvalRequestId"/>. A resubmission after a rejection always carries a new
    /// workflow id, which is what makes a late event for the previous workflow harmless.
    /// </summary>
    public void Submit(
        string requesterUserId,
        string approvalRequestId,
        string approverEmployeeId,
        string? approverName,
        DateTimeOffset now)
    {
        EnsureCanSubmit(requesterUserId);

        if (string.IsNullOrWhiteSpace(approvalRequestId))
            throw Invalid(nameof(approvalRequestId), "An approval request id is required.");

        Status = PurchaseOrderStatus.PendingApproval;
        ApprovalRequestId = approvalRequestId;
        ApproverEmployeeId = approverEmployeeId;
        ApproverName = approverName;
        SubmittedAt = now;
        RejectedAt = null;

        AddDomainEvent(new PurchaseOrderSubmittedEvent(
            Id,
            SupplierId,
            approvalRequestId,
            now));
    }

    /// <summary>The guard part of <see cref="Withdraw"/>, callable before the Approval workflow is cancelled.</summary>
    public void EnsureCanWithdraw(string requesterUserId)
    {
        EnsureRequester(requesterUserId, "withdraw");

        if (Status != PurchaseOrderStatus.PendingApproval)
            throw new ConflictException("Only a purchase order pending approval can be withdrawn.");
    }

    /// <summary>
    /// Requester pulls a pending submission back to <c>Draft</c>. The workflow id is cleared, so a
    /// decision that still arrives for the withdrawn workflow no longer matches and is ignored.
    /// </summary>
    public void Withdraw(string requesterUserId)
    {
        EnsureCanWithdraw(requesterUserId);

        Status = PurchaseOrderStatus.Draft;
        ApprovalRequestId = null;
        ApproverEmployeeId = null;
        ApproverName = null;
        SubmittedAt = null;
    }

    /// <summary>
    /// The single reconcile choke point. Applies a terminal approval decision, returning <c>false</c>
    /// (no-op) when the decision belongs to a superseded workflow
    /// (<paramref name="approvalRequestId"/> mismatch) or the order is no longer pending approval —
    /// so a repeated or late delivery can never un-finalize the order.
    /// </summary>
    public bool ApplyApprovalOutcome(
        string approvalRequestId,
        bool approved,
        DateTimeOffset now)
    {
        if (ApprovalRequestId is null || approvalRequestId != ApprovalRequestId)
            return false;

        if (Status != PurchaseOrderStatus.PendingApproval)
            return false;

        if (approved)
        {
            Status = PurchaseOrderStatus.Approved;
            ApprovedAt = now;

            AddDomainEvent(new PurchaseOrderApprovedEvent(
                Id,
                SupplierId,
                LocationId,
                now));
        }
        else
        {
            Status = PurchaseOrderStatus.Rejected;
            RejectedAt = now;

            AddDomainEvent(new PurchaseOrderRejectedEvent(
                Id,
                SupplierId,
                now));
        }

        return true;
    }

    /// <summary>
    /// Rejects a delivery that cannot be received: the order must be open for receipts and, for every
    /// line, received + quantity still <paramref name="inFlightByLineId"/> (recorded goods receipts whose
    /// stock posting is not confirmed yet) + this delivery must not exceed the ordered quantity.
    /// </summary>
    public void EnsureCanReceive(
        IReadOnlyCollection<(long PurchaseOrderLineId, int Quantity)> lines,
        IReadOnlyDictionary<long, int> inFlightByLineId)
    {
        if (Status is not (PurchaseOrderStatus.Approved or PurchaseOrderStatus.PartiallyReceived))
            throw new ConflictException("Only an approved or partially received purchase order can be received.");

        if (lines.Count == 0)
            throw Invalid("lines", "At least one line must be received.");

        if (lines.Select(x => x.PurchaseOrderLineId).Distinct().Count() != lines.Count)
            throw Invalid("lines", "A purchase order line can appear only once per receipt.");

        foreach (var (purchaseOrderLineId, quantity) in lines)
        {
            var line = FindLine(purchaseOrderLineId);

            if (quantity <= 0)
                throw Invalid("lines", "Every received quantity must be greater than zero.");

            var remaining = line.OutstandingQuantity - inFlightByLineId.GetValueOrDefault(line.Id);

            if (quantity > remaining)
                throw Invalid(
                    "lines",
                    $"Cannot receive {quantity} of '{line.ProductName}': only {Math.Max(remaining, 0)} still outstanding.");
        }
    }

    /// <summary>
    /// Applies a posted goods receipt's quantities. Atomic across lines — every line is validated before
    /// any is changed — and moves the order to <c>PartiallyReceived</c>, or <c>Received</c> once nothing
    /// is outstanding.
    /// </summary>
    public void ApplyReceipt(
        IReadOnlyCollection<(long PurchaseOrderLineId, int Quantity)> lines,
        DateTimeOffset now)
    {
        EnsureCanReceive(lines, new Dictionary<long, int>());

        foreach (var (purchaseOrderLineId, quantity) in lines)
            FindLine(purchaseOrderLineId).ApplyReceived(quantity);

        var fullyReceived = _lines.All(x => x.OutstandingQuantity == 0);

        Status = fullyReceived
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;

        if (fullyReceived)
            ReceivedAt = now;

        AddDomainEvent(new PurchaseOrderReceivedEvent(
            Id,
            LocationId,
            fullyReceived,
            now));
    }

    /// <summary>
    /// Records goods sent back to the supplier against a line. Informational only: the order is never
    /// reopened and the outstanding quantity does not change. Cannot exceed what was received.
    /// </summary>
    public void RegisterReturn(
        long purchaseOrderLineId,
        int quantity)
    {
        var line = FindLine(purchaseOrderLineId);

        if (quantity <= 0)
            throw Invalid(nameof(quantity), "Quantity must be greater than zero.");

        if (quantity > line.ReceivedQuantity - line.ReturnedQuantity)
            throw Invalid(
                nameof(quantity),
                $"Cannot return {quantity} of '{line.ProductName}': only {line.ReceivedQuantity - line.ReturnedQuantity} received and not yet returned.");

        line.ApplyReturned(quantity);
    }

    /// <summary>
    /// Gives up the undelivered remainder of a partially received order. No stock is posted. Refused
    /// while a goods receipt is still being posted (<paramref name="hasReceiptInFlight"/>).
    /// </summary>
    public void Close(
        string reason,
        string closedByUserId,
        DateTimeOffset now,
        bool hasReceiptInFlight)
    {
        if (Status != PurchaseOrderStatus.PartiallyReceived)
            throw new ConflictException("Only a partially received purchase order can be closed.");

        if (hasReceiptInFlight)
            throw new ConflictException("A goods receipt is still being posted; wait for it to finish before closing.");

        Status = PurchaseOrderStatus.Closed;
        ClosedAt = now;
        ClosedReason = reason;
        ClosedBy = closedByUserId;
    }

    /// <summary>
    /// Cancels a draft, a rejected, or an approved order that has not received anything (including a
    /// receipt still being posted, <paramref name="hasReceiptInFlight"/>). An approved order is cancelled
    /// locally only — its approval workflow is already finished. A pending order must be withdrawn first.
    /// A draft or rejected order can be cancelled by its requester or a manager; an approved one only by a
    /// manager (<paramref name="canManage"/>), whoever requested it.
    /// </summary>
    public void Cancel(
        string reason,
        string cancelledByUserId,
        bool canManage,
        DateTimeOffset now,
        bool hasReceiptInFlight)
    {
        switch (Status)
        {
            case PurchaseOrderStatus.Draft:
            case PurchaseOrderStatus.Rejected:
                EnsureOwnerOrManager(cancelledByUserId, canManage);

                break;

            case PurchaseOrderStatus.Approved:
                if (!canManage)
                    throw new ForbiddenException("Only a purchasing manager can cancel an approved purchase order.");

                if (TotalReceivedQuantity > 0 || hasReceiptInFlight)
                    throw new ConflictException("A purchase order that has received goods cannot be cancelled; close it instead.");

                break;

            case PurchaseOrderStatus.PendingApproval:
                throw new ConflictException("Withdraw the purchase order from approval before cancelling it.");

            default:
                throw new ConflictException("This purchase order can no longer be cancelled.");
        }

        Status = PurchaseOrderStatus.Cancelled;
        CancelledAt = now;
        CancelledReason = reason;
        CancelledBy = cancelledByUserId;

        AddDomainEvent(new PurchaseOrderCancelledEvent(
            Id,
            SupplierId,
            now,
            reason));
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>PurchasingDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Called by <c>CreatePurchaseOrderCommandHandler</c>'s bounded retry loop after a generated number
    /// collides with another purchase order's.
    /// </summary>
    internal void RegeneratePONumber(DateTimeOffset now) => PONumber = PurchaseOrderNumber.Generate(now);

    private void ApplyHeader(
        long supplierId,
        string supplierName,
        string locationId,
        string locationName,
        DateTimeOffset? expectedAt,
        string? note)
    {
        SupplierId = supplierId;
        SupplierName = supplierName;
        LocationId = locationId.Trim();
        LocationName = locationName;
        ExpectedAt = expectedAt;
        Note = note;
    }

    private PurchaseOrderLine FindLine(long purchaseOrderLineId)
    {
        var line = _lines.FirstOrDefault(x => x.Id == purchaseOrderLineId);

        if (line is null)
            throw Invalid(nameof(purchaseOrderLineId), "Purchase order line not found on this purchase order.");

        return line;
    }

    private void EnsureEditable()
    {
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Rejected))
            throw new ConflictException("Only a draft or rejected purchase order can be modified.");
    }

    /// <summary>
    /// Editing (header and lines) and cancelling a draft/rejected order is for its requester or a manager
    /// (<paramref name="canManage"/>); anyone else is refused. Submit and withdraw stay requester-only.
    /// </summary>
    public void EnsureOwnerOrManager(
        string userId,
        bool canManage)
    {
        if (!canManage && RequesterUserId != userId)
            throw new ForbiddenException("Only the requester or a purchasing manager can change this purchase order.");
    }

    /// <summary>
    /// A delivery cannot pre-date the order's approval. The future bound on the received date is input
    /// validation, done by the request validator against the clock.
    /// </summary>
    public void EnsureReceivedNotBeforeApproval(DateTimeOffset receivedAt)
    {
        if (ApprovedAt.HasValue && receivedAt < ApprovedAt.Value)
            throw Invalid("receivedAt", "The received date cannot be before the purchase order was approved.");
    }

    private static decimal SaturatingSum(IEnumerable<decimal> amounts)
    {
        var total = 0m;

        foreach (var amount in amounts)
        {
            try
            {
                total = checked(total + amount);
            }
            catch (OverflowException)
            {
                return decimal.MaxValue;
            }
        }

        return total;
    }

    private void EnsureRequester(
        string userId,
        string action)
    {
        if (RequesterUserId != userId)
            throw new ForbiddenException($"Only the requester can {action} this purchase order.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
