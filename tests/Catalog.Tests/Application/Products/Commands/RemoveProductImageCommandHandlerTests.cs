using Catalog.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class RemoveProductImageCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRemoveImage_WhenProductFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id);
        product.AddImage(new ProductImageUrl("https://example.com/a.png"));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        host.Context.ChangeTracker.Clear();
        var handler = new RemoveProductImageCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveProductImageCommand(product.Id, "https://example.com/a.png"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Empty(updated!.Images);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new RemoveProductImageCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveProductImageCommand(999, "https://example.com/a.png"),
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
