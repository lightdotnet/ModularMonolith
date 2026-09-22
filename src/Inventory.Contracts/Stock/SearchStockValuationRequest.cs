namespace StarterKit.Inventory.Contracts.Stock;

public record SearchStockValuationRequest : SearchQuery
{
    public long? ProductId { get; set; }

    public string? LocationId { get; set; }
}
