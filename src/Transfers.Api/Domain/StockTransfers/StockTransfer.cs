using Light.Exceptions;
using StarterKit.Shared.Entities;
using StarterKit.Transfers.Contracts.Common;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Transfers.Api.Domain.StockTransfers;

/// <summary>
/// Aggregate root for moving stock between two locations: a draft is edited, dispatched (stock leaves
/// the source), then received in one or more deliveries — or closed, writing the undelivered remainder
/// off as a local variance. Every state change and quantity rule lives here:
/// <list type="bullet">
/// <item><description><c>Draft</c> → <c>Posting</c> (<see cref="BeginDispatch"/>) → <c>Dispatched</c> (<see cref="CompleteDispatch"/>), or back to <c>Draft</c> if Inventory refuses the issue (<see cref="AbortDispatch"/>).</description></item>
/// <item><description><c>Dispatched</c>/<c>PartiallyReceived</c> → <c>PartiallyReceived</c>/<c>Received</c> through <see cref="BeginReceive"/> + <see cref="CompleteReceive"/>.</description></item>
/// <item><description><c>Dispatched</c>/<c>PartiallyReceived</c> → <c>Closed</c> (<see cref="Close"/>); a dispatched transfer can never be cancelled, only closed.</description></item>
/// <item><description><c>Draft</c> → <c>Cancelled</c> (<see cref="Cancel"/>).</description></item>
/// </list>
/// Source/destination are opaque Location ids (cross-module, no foreign key); their names are
/// snapshots taken by the caller. Stock movements themselves are posted to Inventory by the command
/// handlers around <see cref="BeginDispatch"/>/<see cref="BeginReceive"/> — the aggregate only tracks
/// the resulting quantities and costs.
/// </summary>
public class StockTransfer : AuditableEntity<long>
{
    private readonly List<TransferLine> _lines = [];
    private readonly List<TransferReceipt> _receipts = [];

    private StockTransfer()
    {
    }

    public TransferCode TransferCode { get; private set; } = null!;

    public string SourceLocationId { get; private set; } = null!;

    public string SourceLocationName { get; private set; } = null!;

    public string DestinationLocationId { get; private set; } = null!;

    public string DestinationLocationName { get; private set; } = null!;

    public TransferStatus Status { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>When the transfer entered <see cref="TransferStatus.Posting"/>; drives the reconciliation grace period.</summary>
    public DateTimeOffset? PostingStartedAt { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public DateTimeOffset? ReceivedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public string? ClosedReason { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancelledReason { get; private set; }

    /// <summary>
    /// App-managed optimistic-concurrency token, rotated by <c>TransfersDbContext</c> whenever the
    /// transfer or any of its lines/receipts changes.
    /// </summary>
    public string ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString("N");

    public IReadOnlyList<TransferLine> Lines => _lines.AsReadOnly();

    public IReadOnlyList<TransferReceipt> Receipts => _receipts.AsReadOnly();

    public long TotalRequestedQuantity => _lines.Sum(x => (long)x.RequestedQuantity);

    public long TotalInTransitQuantity => _lines.Sum(x => (long)x.QtyInTransit);

    /// <summary>Base-currency value written off by <see cref="Close"/>.</summary>
    public decimal ClosedShortValueBase => _lines.Sum(x => x.ClosedShortValueBase);

    public static StockTransfer Create(
        string sourceLocationId,
        string sourceLocationName,
        string destinationLocationId,
        string destinationLocationName,
        string? note,
        DateTimeOffset now)
    {
        var transfer = new StockTransfer
        {
            Status = TransferStatus.Draft,
            RequestedAt = now,
            TransferCode = TransferCode.Generate(now),
        };

        transfer.ApplyHeader(
            sourceLocationId,
            sourceLocationName,
            destinationLocationId,
            destinationLocationName,
            note);

        return transfer;
    }

    public void UpdateHeader(
        string sourceLocationId,
        string sourceLocationName,
        string destinationLocationId,
        string destinationLocationName,
        string? note)
    {
        EnsureDraft();

        ApplyHeader(
            sourceLocationId,
            sourceLocationName,
            destinationLocationId,
            destinationLocationName,
            note);
    }

    public void AddLine(
        long productId,
        string productName,
        string sku,
        int quantity)
    {
        EnsureDraft();

        if (_lines.Any(x => x.ProductId == productId))
            throw Invalid(nameof(productId), "This product is already on the transfer.");

        _lines.Add(TransferLine.Create(
            Id,
            productId,
            productName,
            sku,
            quantity));
    }

    public void UpdateLine(
        long transferLineId,
        int quantity)
    {
        EnsureDraft();

        FindLine(transferLineId).UpdateQuantity(quantity);
    }

    public void RemoveLine(long transferLineId)
    {
        EnsureDraft();

        _lines.Remove(FindLine(transferLineId));
    }

    /// <summary>
    /// Fences the transfer as <see cref="TransferStatus.Posting"/> so it can be persisted (and get an
    /// id) before its stock issue is posted to Inventory.
    /// </summary>
    public void BeginDispatch(DateTimeOffset now)
    {
        if (Status != TransferStatus.Draft)
            throw new ConflictException("Only a draft transfer can be dispatched.");

        if (_lines.Count == 0)
            throw Invalid("lines", "At least one transfer line is required.");

        Status = TransferStatus.Posting;
        PostingStartedAt = now;
    }

    /// <summary>
    /// Records the outcome of the Inventory stock issue: freezes every line's dispatched quantity and
    /// unit cost (<paramref name="unitCostByLineId"/>, keyed by transfer line id) and moves the transfer
    /// to <see cref="TransferStatus.Dispatched"/>.
    /// </summary>
    public void CompleteDispatch(
        IReadOnlyDictionary<long, decimal> unitCostByLineId,
        DateTimeOffset now)
    {
        if (Status != TransferStatus.Posting)
            throw new ConflictException("Only a transfer whose dispatch is being posted can be completed.");

        foreach (var line in _lines)
        {
            if (!unitCostByLineId.TryGetValue(line.Id, out var unitCostBase))
                throw Invalid("lines", $"No posted cost was returned for transfer line {line.Id}.");

            line.MarkDispatched(unitCostBase);
        }

        Status = TransferStatus.Dispatched;
        DispatchedAt = now;
        PostingStartedAt = null;

        AddDomainEvent(new TransferDispatchedEvent(
            Id,
            SourceLocationId,
            DestinationLocationId,
            now));
    }

    /// <summary>Returns a transfer whose stock issue was refused (e.g. insufficient stock) to <see cref="TransferStatus.Draft"/>.</summary>
    public void AbortDispatch()
    {
        if (Status != TransferStatus.Posting)
            throw new ConflictException("Only a transfer whose dispatch is being posted can be aborted.");

        Status = TransferStatus.Draft;
        PostingStartedAt = null;
    }

    /// <summary>
    /// Records a delivery as a <see cref="TransferReceiptStatus.Posting"/> receipt and returns it. A
    /// repeated <paramref name="clientRequestId"/> returns the receipt already recorded for it, so a
    /// double-submit never receives twice. Over-receipt is rejected: for every line, already received +
    /// still-posting receipts + this delivery + written off must not exceed what was dispatched.
    /// </summary>
    public TransferReceipt BeginReceive(
        string clientRequestId,
        IReadOnlyCollection<(long TransferLineId, int Quantity)> lines,
        DateTimeOffset now)
    {
        // Trimmed and case-insensitive, matching the (case-insensitive) unique index on the column.
        clientRequestId = clientRequestId.Trim();

        var existing = FindReceiptByClientRequestId(clientRequestId);

        if (existing is not null)
            return existing;

        if (Status is not (TransferStatus.Dispatched or TransferStatus.PartiallyReceived))
            throw new ConflictException("Only a dispatched or partially received transfer can be received.");

        if (lines.Count == 0)
            throw Invalid("lines", "At least one line must be received.");

        if (lines.Select(x => x.TransferLineId).Distinct().Count() != lines.Count)
            throw Invalid("lines", "A transfer line can appear only once per receipt.");

        foreach (var (transferLineId, quantity) in lines)
        {
            var line = FindLine(transferLineId);

            if (quantity <= 0)
                throw Invalid("lines", "Every received quantity must be greater than zero.");

            var awaitingPost = PostingQuantityFor(line.Id);

            if (quantity > line.QtyInTransit - awaitingPost)
                throw Invalid(
                    "lines",
                    $"Cannot receive {quantity} of '{line.ProductName}': only {line.QtyInTransit - awaitingPost} still in transit.");
        }

        var receipt = TransferReceipt.Create(
            Id,
            clientRequestId,
            lines,
            now);

        _receipts.Add(receipt);

        return receipt;
    }

    /// <summary>
    /// Applies a receipt's quantities once Inventory has posted the inbound stock, marks the receipt
    /// <see cref="TransferReceiptStatus.Posted"/>, and moves the transfer to
    /// <see cref="TransferStatus.PartiallyReceived"/> or <see cref="TransferStatus.Received"/>. A no-op
    /// for a receipt that is already posted.
    /// </summary>
    public void CompleteReceive(
        long receiptId,
        DateTimeOffset now)
    {
        var receipt = _receipts.FirstOrDefault(x => x.Id == receiptId)
            ?? throw Invalid(nameof(receiptId), "Receipt not found on this transfer.");

        if (receipt.Status == TransferReceiptStatus.Posted)
            return;

        if (Status is not (TransferStatus.Dispatched or TransferStatus.PartiallyReceived))
            throw new ConflictException("Only a dispatched or partially received transfer can be received.");

        foreach (var receiptLine in receipt.Lines)
            FindLine(receiptLine.TransferLineId).ApplyReceived(receiptLine.Quantity);

        receipt.MarkPosted();

        var fullyReceived = _lines.All(x => x.QtyInTransit == 0);

        Status = fullyReceived
            ? TransferStatus.Received
            : TransferStatus.PartiallyReceived;

        if (fullyReceived)
            ReceivedAt = now;

        AddDomainEvent(new TransferReceivedEvent(
            Id,
            receipt.Id,
            DestinationLocationId,
            fullyReceived,
            now));
    }

    /// <summary>
    /// Abandons a receipt whose inbound posting Inventory deterministically refused (and nothing
    /// landed), so it stops counting as pending and no longer blocks <see cref="Close"/>. Only a
    /// <see cref="TransferReceiptStatus.Posting"/> receipt can be voided; the caller is responsible for
    /// having confirmed with Inventory that no posting landed. Nothing is ever reversed in Inventory.
    /// </summary>
    public void VoidReceipt(
        long receiptId,
        string reason,
        DateTimeOffset now)
    {
        var receipt = _receipts.FirstOrDefault(x => x.Id == receiptId)
            ?? throw Invalid(nameof(receiptId), "Receipt not found on this transfer.");

        if (receipt.Status != TransferReceiptStatus.Posting)
            throw new ConflictException("Only a receipt that is still being posted can be voided.");

        receipt.MarkVoided(reason, now);
    }

    /// <summary>Looks a receipt up by its client request id (trimmed, case-insensitive), whatever its status.</summary>
    public TransferReceipt? FindReceiptByClientRequestId(string clientRequestId)
    {
        clientRequestId = clientRequestId.Trim();

        return _receipts.FirstOrDefault(x => string.Equals(
            x.ClientRequestId,
            clientRequestId,
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Writes off everything still in transit as a local variance (<see cref="TransferLine.QtyClosedShort"/>
    /// and its value) — no stock is posted to Inventory. Only for a dispatched or partially received
    /// transfer that still has quantity outstanding and no delivery mid-posting.
    /// </summary>
    public void Close(
        string reason,
        DateTimeOffset now)
    {
        if (Status is not (TransferStatus.Dispatched or TransferStatus.PartiallyReceived))
            throw new ConflictException("Only a dispatched or partially received transfer can be closed.");

        if (_receipts.Any(x => x.Status == TransferReceiptStatus.Posting))
            throw new ConflictException("A receipt is still being posted; wait for it to finish before closing.");

        if (TotalInTransitQuantity <= 0)
            throw new ConflictException("Nothing is outstanding on this transfer.");

        foreach (var line in _lines)
            line.CloseShort();

        Status = TransferStatus.Closed;
        ClosedAt = now;
        ClosedReason = reason;

        AddDomainEvent(new TransferClosedEvent(
            Id,
            SourceLocationId,
            DestinationLocationId,
            now,
            reason,
            ClosedShortValueBase));
    }

    /// <summary>Only a draft can be cancelled; a dispatched transfer has to be closed instead.</summary>
    public void Cancel(
        string reason,
        DateTimeOffset now)
    {
        if (Status != TransferStatus.Draft)
            throw new ConflictException("Only a draft transfer can be cancelled; close a dispatched transfer instead.");

        Status = TransferStatus.Cancelled;
        CancelledAt = now;
        CancelledReason = reason;

        AddDomainEvent(new TransferCancelledEvent(
            Id,
            SourceLocationId,
            DestinationLocationId,
            now,
            reason));
    }

    /// <summary>Rotates the optimistic-concurrency token; called by <c>TransfersDbContext</c> on save.</summary>
    internal void RotateConcurrencyToken() => ConcurrencyToken = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Called by <c>CreateStockTransferCommandHandler</c>'s bounded retry loop after a generated
    /// <see cref="TransferCode"/> collides with another transfer's.
    /// </summary>
    internal void RegenerateTransferCode(DateTimeOffset now) => TransferCode = TransferCode.Generate(now);

    private void ApplyHeader(
        string sourceLocationId,
        string sourceLocationName,
        string destinationLocationId,
        string destinationLocationName,
        string? note)
    {
        sourceLocationId = sourceLocationId.Trim();
        destinationLocationId = destinationLocationId.Trim();

        if (string.Equals(sourceLocationId, destinationLocationId, StringComparison.OrdinalIgnoreCase))
            throw Invalid(nameof(destinationLocationId), "The source and destination locations must differ.");

        SourceLocationId = sourceLocationId;
        SourceLocationName = sourceLocationName;
        DestinationLocationId = destinationLocationId;
        DestinationLocationName = destinationLocationName;
        Note = note;
    }

    /// <summary>Quantity of a line covered by receipts that are recorded but not yet applied to the line.</summary>
    private int PostingQuantityFor(long transferLineId) =>
        _receipts
            .Where(x => x.Status == TransferReceiptStatus.Posting)
            .SelectMany(x => x.Lines)
            .Where(x => x.TransferLineId == transferLineId)
            .Sum(x => x.Quantity);

    private TransferLine FindLine(long transferLineId)
    {
        var line = _lines.FirstOrDefault(x => x.Id == transferLineId);

        if (line is null)
            throw Invalid(nameof(transferLineId), "Transfer line not found on this transfer.");

        return line;
    }

    private void EnsureDraft()
    {
        if (Status != TransferStatus.Draft)
            throw new ConflictException("This transfer is no longer a draft and cannot be modified.");
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
