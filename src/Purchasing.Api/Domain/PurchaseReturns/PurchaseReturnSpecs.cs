namespace StarterKit.Purchasing.Api.Domain.PurchaseReturns;

public class PurchaseReturnByIdSpec : Specification<PurchaseReturn>
{
    public PurchaseReturnByIdSpec(long purchaseReturnId)
    {
        Where(x => x.Id == purchaseReturnId);
    }
}
