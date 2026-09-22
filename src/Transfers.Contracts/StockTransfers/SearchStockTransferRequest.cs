using StarterKit.Transfers.Contracts.Common;

namespace StarterKit.Transfers.Contracts.StockTransfers;

/// <summary><see cref="SearchQuery.SearchValue"/> matches the transfer code.</summary>
public record SearchStockTransferRequest : SearchQuery
{
    public TransferStatus? Status { get; set; }

    public string? SourceLocationId { get; set; }

    public string? DestinationLocationId { get; set; }
}
