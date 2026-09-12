using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Contracts.Categories;
using Xunit;

namespace Catalog.Tests.Application.Categories.Commands;

public class MoveCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new MoveCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveCategoryCommand("missing", new MoveCategoryRequest { NewParentCategoryId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldClearParent_WhenNewParentCategoryIdIsEmpty()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var parent = await SeedCategoryAsync(host, "Parent", null);
        var child = await SeedCategoryAsync(host, "Child", parent.Id);
        var handler = new MoveCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveCategoryCommand(child.Id, new MoveCategoryRequest { NewParentCategoryId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Categories.FindAsync([child.Id], TestContext.Current.CancellationToken);
        Assert.Null(updated!.ParentCategoryId);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentIsSelf()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics", null);
        var handler = new MoveCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveCategoryCommand(category.Id, new MoveCategoryRequest { NewParentCategoryId = category.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenNewParentDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics", null);
        var handler = new MoveCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveCategoryCommand(category.Id, new MoveCategoryRequest { NewParentCategoryId = "missing" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenNewParentIsOwnDescendant()
    {
        // Arrange: root -> child -> grandchild; moving root under grandchild must be rejected.
        using var host = new CatalogTestHost();
        var root = await SeedCategoryAsync(host, "Root", null);
        var child = await SeedCategoryAsync(host, "Child", root.Id);
        var grandchild = await SeedCategoryAsync(host, "Grandchild", child.Id);
        var handler = new MoveCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveCategoryCommand(root.Id, new MoveCategoryRequest { NewParentCategoryId = grandchild.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldMove_WhenTargetIsValidNewParent()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var oldParent = await SeedCategoryAsync(host, "Old Parent", null);
        var newParent = await SeedCategoryAsync(host, "New Parent", null);
        var child = await SeedCategoryAsync(host, "Child", oldParent.Id);
        var handler = new MoveCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MoveCategoryCommand(child.Id, new MoveCategoryRequest { NewParentCategoryId = newParent.Id }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Categories.FindAsync([child.Id], TestContext.Current.CancellationToken);
        Assert.Equal(newParent.Id, updated!.ParentCategoryId);
    }

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host, string name, string? parentId)
    {
        var category = Category.Create(name, parentId);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }
}
