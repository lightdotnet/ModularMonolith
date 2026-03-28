namespace Monolith.Catalog.Domain.Products;

internal class ProductByIdSpec : Specification<Product>
{
    public ProductByIdSpec(string id)
    {
        Where(x => x.Id == id);
    }
}
