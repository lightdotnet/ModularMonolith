using Microsoft.Extensions.Logging;
using Monolith.Catalog.Domain.Products;
using Monolith.Catalog.Domain.Shops;
using Monolith.Database;

namespace Monolith.Catalog.Infrastructure.Data.SeedWork;

public class CatalogContextInitialiser(
    CatalogContext context,
    ILogger<CatalogContextInitialiser> logger)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }

    public async Task TrySeedAsync()
    {
        logger.LogInformation("catalog_module seeding data...");

        try
        {
            if (await context.Database.CanConnectAsync())
            {
                await SeedAsync();
                logger.LogInformation("catalog_module seed data completed");
            }
            else
            {
                logger.LogError("catalog_module cannot connect to DB");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "catalog_module seeding data error: {mess}", ex.Message);
            throw;
        }
    }

    public async Task SeedAsync()
    {
        var shops = new[]
        {
            Shop.Create("Shop 1 Test"),
            Shop.Create("Shop 2 Test"),
            Shop.Create("Shop 3 Test"),
        };

        await context.Set<Shop>().AddRangeAsync(shops);

        var drinkCategory = Category.Create("Drinks");
        var foodCategory = Category.Create("Foods");

        var categories = new Category[]
        {
            drinkCategory,
            foodCategory,
            Category.Create("Gas", drinkCategory.Id),
            Category.Create("Alcohol", drinkCategory.Id),
            Category.Create("Bread", foodCategory.Id),
            Category.Create("Rice", foodCategory.Id),
        };

        await context.Categories.AddRangeAsync(categories);

        var productImages = new List<string>()
        {
            "https://images.pexels.com/photos/90946/pexels-photo-90946.jpeg",
            "https://images.pexels.com/photos/335257/pexels-photo-335257.jpeg",
            "https://images.pexels.com/photos/3907507/pexels-photo-3907507.jpeg"
        };

        var products = new List<Product>();

        for (var i = 1; i <= 100; i++)
        {
            var random = new Random();

            var product = Product.Create(shops[random.Next(0, 2)].Id, categories[random.Next(2, 5)].Id, $"Product Name {i}", $"Product Description {i}");
            product.MainImage = productImages.First();
            product.UpdateCode($"{100000 + i}");
            product.UpdateImages(productImages);

            products.Add(product);
        }

        await context.Products.AddRangeAsync(products);

        await context.SaveChangesAsync();
    }
}
