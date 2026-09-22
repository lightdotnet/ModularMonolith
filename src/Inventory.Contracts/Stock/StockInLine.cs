namespace StarterKit.Inventory.Contracts.Stock;

/// <summary>One line of a generic stock receipt; <see cref="IdempotencyRef"/> is unique within its source document.</summary>
public sealed record StockInLine(
    long ProductId,
    long SourceLineId,
    int Quantity,
    decimal UnitCostBase,
    string IdempotencyRef);
