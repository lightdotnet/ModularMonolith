namespace StarterKit.Inventory.Contracts.Stock;

public sealed record StockLine(
    long ProductId,
    long OrderLineId,
    int Quantity);
