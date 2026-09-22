namespace StarterKit.Inventory.Contracts.Stock;

public class StockLevelDto : BaseDto<long>
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    public int QuantityOnHand { get; set; }

    /// <summary>Moving-average unit cost (base currency); null unless the caller has <c>Inventory.ViewCost</c>.</summary>
    public decimal? AverageCostBase { get; set; }

    /// <summary>Total stock value (base currency); null unless the caller has <c>Inventory.ViewCost</c>.</summary>
    public decimal? TotalValueBase { get; set; }
}
