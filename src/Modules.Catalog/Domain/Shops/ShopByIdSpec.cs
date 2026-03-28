namespace Monolith.Catalog.Domain.Shops;

internal class ShopByIdSpec : Specification<Shop>
{
    public ShopByIdSpec(string id)
    {
        Where(x => x.Id == id);
    }
}
