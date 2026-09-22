namespace StarterKit.Inventory.Contracts.Stock;

/// <summary>
/// Stock valuation: one page of per product/location lines plus the grand totals over EVERY line that
/// matches the filter (not just the page). All values are base currency.
/// </summary>
public class StockValuationDto
{
    public IReadOnlyList<StockValuationLineDto> Lines { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    public long GrandTotalQuantity { get; set; }

    public decimal GrandTotalValueBase { get; set; }
}

public class StockValuationLineDto
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    public int QuantityOnHand { get; set; }

    public decimal AverageCostBase { get; set; }

    public decimal TotalValueBase { get; set; }
}
