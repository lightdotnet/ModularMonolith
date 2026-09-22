namespace StarterKit.Catalog.Api.Domain.Products;

public class ProductByIdSpec : Specification<Product>
{
    public ProductByIdSpec(long productId)
    {
        Where(x => x.Id == productId);
    }
}
