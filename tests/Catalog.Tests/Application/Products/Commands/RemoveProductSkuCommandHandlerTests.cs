using Catalog.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class RemoveProductSkuCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new RemoveProductSkuCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new RemoveProductSkuCommand(999), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldClearTheSku_WhenProductIsActive()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id, "SKU-001");
        var handler = new RemoveProductSkuCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new RemoveProductSkuCommand(product.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        Assert.Null(updated!.Sku);
    }

    [Fact]
    public async Task Handle_ShouldClearTheSku_WhenProductIsAlreadySoftDeleted()
    {
        // Arrange — the exact scenario this endpoint exists for: freeing the SKU of a soft-deleted
        // product for reuse must work even though the default query filter hides it, because the
        // handler loads via IgnoreQueryFilters().
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id, "SKU-001");
        product.Deactivate();
        product.Delete();
        host.Context.Products.Remove(product);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveProductSkuCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new RemoveProductSkuCommand(product.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Products
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Null(reloaded.Sku);
        Assert.NotNull(reloaded.Deleted);
    }

    [Fact]
    public async Task Sku_ShouldBeReusable_ByANewProduct_OnceClearedFromASoftDeletedProduct()
    {
        // Arrange — end-to-end through CreateProductCommandHandler + RemoveProductSkuCommandHandler,
        // covering the filtered unique index behavior described in CatalogDbContext: soft-delete
        // alone does not free the SKU, an explicit ClearSku does.
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var oldProduct = await SeedProductAsync(host, category.Id, "SHARED-SKU");
        oldProduct.Deactivate();
        oldProduct.Delete();
        host.Context.Products.Remove(oldProduct);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createHandler = new CreateProductCommandHandler(host.Context);
        var removeSkuHandler = new RemoveProductSkuCommandHandler(host.Context);

        // Act 1 — still blocked: the soft-deleted product's SKU has not been cleared yet.
        var blockedResult = await createHandler.Handle(
            new CreateProductCommand(ValidRequest(category.Id, "SHARED-SKU")),
            TestContext.Current.CancellationToken);

        // Assert 1
        Assert.False(blockedResult.IsSuccess);

        // Act 2 — clear the old product's SKU, then retry.
        await removeSkuHandler.Handle(new RemoveProductSkuCommand(oldProduct.Id), TestContext.Current.CancellationToken);
        var allowedResult = await createHandler.Handle(
            new CreateProductCommand(ValidRequest(category.Id, "SHARED-SKU")),
            TestContext.Current.CancellationToken);

        // Assert 2
        Assert.True(allowedResult.IsSuccess);
        var newProduct = await host.Context.Products.FindAsync([allowedResult.Data], TestContext.Current.CancellationToken);
        Assert.Equal("SHARED-SKU", newProduct!.Sku!.Value);
    }

    private static CreateProductRequest ValidRequest(string categoryId, string sku) =>
        new()
        {
            CategoryId = categoryId,
            Name = "New Widget",
            Sku = sku,
            Price = 50m,
            Currency = CurrencyConstants.Default,
            VatRate = 5m,
        };

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
