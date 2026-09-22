namespace StarterKit.Inventory.Contracts.Stock;

public record SearchStockAdjustmentRequest : SearchQuery
{
    public long? ProductId { get; set; }

    public string? LocationId { get; set; }

    public long? SourceOrderId { get; set; }
}
