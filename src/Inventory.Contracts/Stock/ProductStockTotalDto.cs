namespace StarterKit.Inventory.Contracts.Stock;

public class ProductStockTotalDto
{
    public long ProductId { get; set; }

    public int TotalQuantityOnHand { get; set; }

    public int LocationCount { get; set; }
}
