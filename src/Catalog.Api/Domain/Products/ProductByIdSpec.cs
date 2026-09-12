namespace StarterKit.Catalog.Api.Domain.Products;

public class ProductByIdSpec : Specification<Product>
{
    public ProductByIdSpec(string productId)
    {
        Where(x => x.Id == productId);
    }
}
