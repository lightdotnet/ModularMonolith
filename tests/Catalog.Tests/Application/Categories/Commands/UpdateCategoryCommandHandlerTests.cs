using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Contracts.Categories;
using Xunit;

namespace Catalog.Tests.Application.Categories.Commands;

public class UpdateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRenameCategory_WhenRequestIsValid()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics", null);
        var handler = new UpdateCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateCategoryCommand(category.Id, new UpdateCategoryRequest { Name = "Consumer Electronics" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Categories.FindAsync([category.Id], TestContext.Current.CancellationToken);
        Assert.Equal("Consumer Electronics", updated!.Name);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new UpdateCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateCategoryCommand("missing", new UpdateCategoryRequest { Name = "Whatever" }),
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
