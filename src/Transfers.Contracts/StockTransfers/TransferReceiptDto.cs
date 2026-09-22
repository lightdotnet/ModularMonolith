using StarterKit.Transfers.Contracts.Common;

namespace StarterKit.Transfers.Contracts.StockTransfers;

public class TransferReceiptDto : BaseDto<long>
{
    public string ClientRequestId { get; set; } = null!;

    public TransferReceiptStatus Status { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset? VoidedAt { get; set; }

    public string? VoidReason { get; set; }

    public IList<TransferReceiptLineDto> Lines { get; set; } = [];
}

public class TransferReceiptLineDto : BaseDto<long>
{
    public long TransferLineId { get; set; }

    public int Quantity { get; set; }
}
