namespace StarterKit.Purchasing.Api.Domain.Suppliers;

public class SupplierByIdSpec : Specification<Supplier>
{
    public SupplierByIdSpec(long supplierId)
    {
        Where(x => x.Id == supplierId);
    }
}
