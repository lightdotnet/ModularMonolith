using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Api.Services;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Services;

public class CatalogPricingServiceTests
{
    [Fact]
    public async Task GetPriceInfoAsync_ShouldReturnPriceInfo_WhenProductFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id, "SKU-001");
        var service = new CatalogPricingService(host.Context);

        // Act
        var result = await service.GetPriceInfoAsync(product.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SKU-001", result.Sku);
        Assert.Equal(100m, result.Price);
        Assert.Equal(CurrencyConstants.Default, result.Currency);
        Assert.Equal(10m, result.VatRate);
    }

    [Fact]
    public async Task GetPriceInfoAsync_ShouldReturnNull_WhenProductNotFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var service = new CatalogPricingService(host.Context);

        // Act
        var result = await service.GetPriceInfoAsync(999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPriceInfoAsync_ShouldReturnNull_WhenProductIsSoftDeleted()
    {
        // Arrange — CatalogDbContext's Product query filter (Deleted == null) applies transparently
        // here too, since this service does not call IgnoreQueryFilters().
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id, "SKU-001");
        product.Deactivate();
        product.Delete();
        host.Context.Products.Remove(product);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new CatalogPricingService(host.Context);

        // Act
        var result = await service.GetPriceInfoAsync(product.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPriceInfoBatchAsync_ShouldReturnOnlyMatchingIds()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var productA = await SeedProductAsync(host, category.Id, "SKU-A");
        var productB = await SeedProductAsync(host, category.Id, "SKU-B");
        var service = new CatalogPricingService(host.Context);

        // Act
        var result = await service.GetPriceInfoBatchAsync([productA.Id, 999], TestContext.Current.CancellationToken);

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(productA.Id, item.Id);
        _ = productB;
    }

    [Fact]
    public async Task GetPriceInfoBatchAsync_ShouldReturnEmpty_WhenNoneFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var service = new CatalogPricingService(host.Context);

        // Act
        var result = await service.GetPriceInfoBatchAsync([998, 999], TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host)
    {
        var category = Category.Create("Electronics", null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }

    private static async Task<Product> SeedProductAsync(CatalogTestHost host, string categoryId, string sku)
    {
        var product = Product.Create(
            categoryId,
            "Widget",
            null,
            new Sku(sku),
            new Money(100m, CurrencyConstants.Default),
            new VatPercentage(10m));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }
}
