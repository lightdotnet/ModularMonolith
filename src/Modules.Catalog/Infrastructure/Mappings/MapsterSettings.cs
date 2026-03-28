using Mapster;
using Monolith.Catalog.Domain.Products;

namespace Monolith.Catalog.Infrastructure.Mappings;

internal class MapsterSettings
{
    public static void Configure()
    {
        // here we will define the type conversion / Custom-mapping
        // More details at https://github.com/MapsterMapper/Mapster/wiki/Custom-mapping

        // This one is actually not necessary as it's mapped by convention

        TypeAdapterConfig<Product, ProductDto>
            .NewConfig()
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.Images, src => src.Images.Select(s => s.ImageUrl));
    }
}