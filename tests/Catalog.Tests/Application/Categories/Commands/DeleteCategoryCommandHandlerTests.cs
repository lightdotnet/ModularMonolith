using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Categories.Commands;

public class DeleteCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDeleteCategory_WhenItHasNoChildrenOrProducts()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics", null);
        var handler = new DeleteCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new DeleteCategoryCommand(category.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        Assert.Null(entity);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new DeleteCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new DeleteCategoryCommand("missing"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCategoryHasChildCategories()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var parent = await SeedCategoryAsync(host, "Parent", null);
        await SeedCategoryAsync(host, "Child", parent.Id);
        var handler = new DeleteCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new DeleteCategoryCommand(parent.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCategoryHasProductsAssigned()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics", null);
        var product = Product.Create(
            category.Id,
            "Widget",
            null,
            new Sku("SKU-001"),
            new Money(100m, CurrencyConstants.Default),
            new VatPercentage(10m));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new DeleteCategoryCommand(category.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host, string name, string? parentId)
    {
        var category = Category.Create(name, parentId);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }
}
