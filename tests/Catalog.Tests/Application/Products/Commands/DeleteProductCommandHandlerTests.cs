using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class DeleteProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new DeleteProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteProductCommand(999), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_WhenProductIsActive()
    {
        // Arrange — Product.Delete's own guard throws directly; this handler has no try/catch
        // translation, so the domain exception propagates as-is (mirrors
        // MarkOrderFulfilledCommandHandlerTests in Orders.Tests).
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id);
        var handler = new DeleteProductCommandHandler(host.Context);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new DeleteProductCommand(product.Id),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteProduct_WhenProductIsInactive()
    {
        // Arrange
        using var host = new CatalogTestHost(new FakeCurrentUser { UserId = "user-1" });
        var category = await SeedCategoryAsync(host);
        var product = await SeedProductAsync(host, category.Id);
        product.Deactivate();
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(new DeleteProductCommand(product.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);

        // The default query filter (Deleted == null) hides the now-soft-deleted product...
        var visibleThroughDefaultQuery = await host.Context.Products
            .FirstOrDefaultAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Null(visibleThroughDefaultQuery);

        // ...but the row is still there (a real soft-delete, not a real DELETE), stamped by
        // TrackingExtensions.AuditEntries via the Product.Delete -> context.Products.Remove(entity)
        // interception, reachable only through IgnoreQueryFilters.
        var reloaded = await host.Context.Products
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(reloaded.Deleted);
        Assert.Equal("user-1", reloaded.DeletedBy);
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
