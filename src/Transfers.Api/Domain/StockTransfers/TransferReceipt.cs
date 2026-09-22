using StarterKit.Shared.Entities;
using StarterKit.Transfers.Contracts.Common;

namespace StarterKit.Transfers.Api.Domain.StockTransfers;

/// <summary>
/// One inbound delivery against a <see cref="StockTransfer"/>. <see cref="ClientRequestId"/> is unique
/// per transfer, so a resubmitted request maps back onto the same receipt instead of receiving twice.
/// A receipt is recorded as <see cref="TransferReceiptStatus.Posting"/> first (so it has an id to post
/// to Inventory under) and only becomes <see cref="TransferReceiptStatus.Posted"/> once the transfer's
/// line quantities have been updated (<see cref="StockTransfer.CompleteReceive"/>); a receipt whose
/// posting Inventory refused outright ends as <see cref="TransferReceiptStatus.Voided"/>
/// (<see cref="StockTransfer.VoidReceipt"/>).
/// </summary>
public class TransferReceipt : AuditableEntity<long>
{
    private readonly List<TransferReceiptLine> _lines = [];

    private TransferReceipt()
    {
    }

    public long TransferId { get; private set; }

    public string ClientRequestId { get; private set; } = null!;

    public TransferReceiptStatus Status { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public string? VoidReason { get; private set; }

    public virtual StockTransfer Transfer { get; private set; } = null!;

    public IReadOnlyList<TransferReceiptLine> Lines => _lines.AsReadOnly();

    internal static TransferReceipt Create(
        long transferId,
        string clientRequestId,
        IEnumerable<(long TransferLineId, int Quantity)> lines,
        DateTimeOffset now)
    {
        var receipt = new TransferReceipt
        {
            TransferId = transferId,
            ClientRequestId = clientRequestId,
            Status = TransferReceiptStatus.Posting,
            ReceivedAt = now,
        };

        foreach (var (transferLineId, quantity) in lines)
            receipt._lines.Add(TransferReceiptLine.Create(transferLineId, quantity));

        return receipt;
    }

    internal void MarkPosted() => Status = TransferReceiptStatus.Posted;

    internal void MarkVoided(
        string reason,
        DateTimeOffset now)
    {
        Status = TransferReceiptStatus.Voided;
        VoidReason = reason;
        VoidedAt = now;
    }
}

/// <summary>Quantity received for one <see cref="TransferLine"/> within a <see cref="TransferReceipt"/>.</summary>
public class TransferReceiptLine : AuditableEntity<long>
{
    private TransferReceiptLine()
    {
    }

    public long ReceiptId { get; private set; }

    public long TransferLineId { get; private set; }

    public int Quantity { get; private set; }

    public virtual TransferReceipt Receipt { get; private set; } = null!;

    internal static TransferReceiptLine Create(
        long transferLineId,
        int quantity) =>
        new()
        {
            TransferLineId = transferLineId,
            Quantity = quantity,
        };
}
