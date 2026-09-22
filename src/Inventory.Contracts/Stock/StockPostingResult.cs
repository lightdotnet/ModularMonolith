namespace StarterKit.Inventory.Contracts.Stock;

/// <summary>
/// The cost Inventory posted for one source line, always in base currency: the unit cost and total
/// value that entered stock (receive) or left it at the moving average (issue / order placement).
/// </summary>
public sealed record StockPostingResult(
    long ProductId,
    long SourceLineId,
    int Quantity,
    decimal UnitCostBase,
    decimal ValueBase);
