namespace StarterKit.Transfers.Contracts.StockTransfers;

public class TransferLineDto : BaseDto<long>
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public int RequestedQuantity { get; set; }

    public int QtyDispatched { get; set; }

    public int QtyReceived { get; set; }

    public int QtyClosedShort { get; set; }

    /// <summary>Dispatched, not yet received and not written off.</summary>
    public int QtyInTransit { get; set; }

    public decimal? UnitCostBase { get; set; }

    public decimal? ClosedShortValueBase { get; set; }
}
