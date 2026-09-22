using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class AddProductImageCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldAddImage_WhenProductFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id);
        var handler = new AddProductImageCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new AddProductImageCommand(product.Id, new AddProductImageRequest { Url = "https://example.com/a.png", SortOrder = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        var image = Assert.Single(updated!.Images);
        Assert.Equal("https://example.com/a.png", image.Url);
        Assert.Equal(1, image.SortOrder);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new AddProductImageCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new AddProductImageCommand(999, new AddProductImageRequest { Url = "https://example.com/a.png" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host)
    {
        var category = Category.Create("Electronics", null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }

    private static async Task<Product> SeedProductAsync(CatalogTestHost host, string categoryId)
    {
        var product = Product.Create(
            categoryId,
            "Widget",
            null,
            new Sku("SKU-001"),
            new Money(100m, CurrencyConstants.Default),
            new VatPercentage(10m));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }
}
