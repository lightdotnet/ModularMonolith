namespace StarterKit.Transfers.Api.Domain.StockTransfers;

public class StockTransferByIdSpec : Specification<StockTransfer>
{
    public StockTransferByIdSpec(long transferId)
    {
        Where(x => x.Id == transferId);
    }
}
