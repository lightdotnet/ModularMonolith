namespace StarterKit.Inventory.Contracts.Stock;

/// <summary>One line of a generic stock issue; <see cref="IdempotencyRef"/> is unique within its source document.</summary>
public sealed record StockOutLine(
    long ProductId,
    long SourceLineId,
    int Quantity,
    string IdempotencyRef);
