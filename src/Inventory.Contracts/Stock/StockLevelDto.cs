namespace StarterKit.Inventory.Contracts.Stock;

public class StockLevelDto : BaseDto<long>
{
    public long ProductId { get; set; }

    public string LocationId { get; set; } = null!;

    public int QuantityOnHand { get; set; }
}
