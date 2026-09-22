using Microsoft.Extensions.Logging;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Persistence.MigrationSupport;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace StarterKit.Catalog.Api.Data;

public class CatalogContextInitialiser(
    ILogger<CatalogContextInitialiser> logger,
    CatalogDbContext context)
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
        var beverages = await GetOrCreateCategoryAsync("Beverages", null);
        var softDrinks = await GetOrCreateCategoryAsync("Soft Drinks", beverages.Id);
        var coffeeAndTea = await GetOrCreateCategoryAsync("Coffee & Tea", beverages.Id);

        var snacks = await GetOrCreateCategoryAsync("Snacks", null);

        var household = await GetOrCreateCategoryAsync("Household", null);
        var cleaningSupplies = await GetOrCreateCategoryAsync("Cleaning Supplies", household.Id);

        await GetOrCreateProductAsync(
            softDrinks.Id, "Coca-Cola 330ml", "Canned cola soft drink, 330ml.", "CD-COKE-330", 12000m, 8m);
        await GetOrCreateProductAsync(
            softDrinks.Id, "Pepsi 330ml", "Canned cola soft drink, 330ml.", "CD-PEPSI-330", 12000m, 8m);

        await GetOrCreateProductAsync(
            coffeeAndTea.Id, "Instant Coffee 100g", "Instant coffee powder, 100g jar.", "CT-COFFEE-100", 45000m, 8m);
        await GetOrCreateProductAsync(
            coffeeAndTea.Id, "Green Tea Bags Box", "Box of 25 green tea bags.", "CT-TEA-25", 35000m, 8m);

        await GetOrCreateProductAsync(
            snacks.Id, "Potato Chips 100g", "Salted potato chips, 100g bag.", "SN-CHIPS-100", 18000m, 8m);
        await GetOrCreateProductAsync(
            snacks.Id, "Chocolate Bar 45g", "Milk chocolate bar, 45g.", "SN-CHOC-45", 15000m, 8m);

        await GetOrCreateProductAsync(
            cleaningSupplies.Id, "Dish Soap 500ml", "Dishwashing liquid, 500ml bottle.", "HH-DISH-500", 28000m, 10m);
        await GetOrCreateProductAsync(
            cleaningSupplies.Id, "Laundry Detergent 1L", "Liquid laundry detergent, 1L bottle.", "HH-LAUNDRY-1L", 65000m, 10m);
    }

    private async Task<Category> GetOrCreateCategoryAsync(string name, string? parentCategoryId)
    {
        var existing = await context.Categories
            .SingleOrDefaultAsync(x => x.ParentCategoryId == parentCategoryId && x.Name == name);

        if (existing is not null)
            return existing;

        var category = Category.Create(name, parentCategoryId);

        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        logger.LogInformation("Category {name} added", name);

        return category;
    }

    private async Task<Product> GetOrCreateProductAsync(
        string categoryId,
        string name,
        string description,
        string sku,
        decimal price,
        decimal vatRate)
    {
        var skuVo = new Sku(sku);

        var existing = await context.Products.SingleOrDefaultAsync(x => x.Sku == skuVo);

        if (existing is not null)
            return existing;

        var product = Product.Create(
            categoryId,
            name,
            description,
            skuVo,
            new Money(price, CurrencyConstants.Default),
            new VatPercentage(vatRate));

        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        logger.LogInformation("Product {sku} added", sku);

        return product;
    }
}
