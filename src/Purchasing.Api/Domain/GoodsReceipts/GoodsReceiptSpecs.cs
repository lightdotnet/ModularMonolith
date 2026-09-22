namespace StarterKit.Purchasing.Api.Domain.GoodsReceipts;

public class GoodsReceiptByIdSpec : Specification<GoodsReceipt>
{
    public GoodsReceiptByIdSpec(long goodsReceiptId)
    {
        Where(x => x.Id == goodsReceiptId);
    }
}
