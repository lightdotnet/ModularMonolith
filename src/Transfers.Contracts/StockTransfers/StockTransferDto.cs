using StarterKit.Transfers.Contracts.Common;

namespace StarterKit.Transfers.Contracts.StockTransfers;

/// <summary>
/// Cost fields (<see cref="ClosedShortValueBase"/> and the per-line unit cost / closed-short value)
/// are null unless the caller may view stock costs.
/// </summary>
public class StockTransferDto : BaseDto<long>
{
    public string TransferCode { get; set; } = null!;

    public string SourceLocationId { get; set; } = null!;

    public string SourceLocationName { get; set; } = null!;

    public string DestinationLocationId { get; set; } = null!;

    public string DestinationLocationName { get; set; } = null!;

    public TransferStatus Status { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? DispatchedAt { get; set; }

    public DateTimeOffset? ReceivedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string? ClosedReason { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancelledReason { get; set; }

    public long TotalRequestedQuantity { get; set; }

    public long TotalInTransitQuantity { get; set; }

    public decimal? ClosedShortValueBase { get; set; }

    public IList<TransferLineDto> Lines { get; set; } = [];

    public IList<TransferReceiptDto> Receipts { get; set; } = [];
}
