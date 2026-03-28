using Monolith.Catalog.Domain.Products;

namespace Monolith.Catalog.UseCases.Products;

internal class ProductLookupSpec : Specification<Product>
{
    public ProductLookupSpec(ProductLookup lookup)
    {
        WhereIf(!string.IsNullOrEmpty(lookup.Search), x => EF.Functions.Like(x.Tags, $"%{lookup.Search}%"));

        WhereIf(!string.IsNullOrEmpty(lookup.ShopId), x => x.ShopId == lookup.ShopId);
    }
}
