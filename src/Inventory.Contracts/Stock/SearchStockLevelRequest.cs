namespace StarterKit.Inventory.Contracts.Stock;

public record SearchStockLevelRequest : SearchQuery
{
    public long? ProductId { get; set; }

    public string? LocationId { get; set; }
}
