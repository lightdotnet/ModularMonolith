using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Contracts.Categories;
using Xunit;

namespace Catalog.Tests.Application.Categories.Commands;

public class CreateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateCategory_WhenRequestIsValid()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new CreateCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateCategoryCommand(new CreateCategoryRequest { Name = "Electronics", ParentCategoryId = null }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.Categories.FindAsync([result.Data], TestContext.Current.CancellationToken);
        Assert.Equal("Electronics", entity!.Name);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenParentDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new CreateCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateCategoryCommand(new CreateCategoryRequest { Name = "Electronics", ParentCategoryId = "missing" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenSiblingNameAlreadyExistsUnderSameParent()
    {
        // Arrange: non-root case — both categories share the same non-null ParentCategoryId, so the
        // unique index (which doesn't constrain two NULL rows) does apply here.
        using var host = new CatalogTestHost();
        var parent = await SeedCategoryAsync(host, "Parent", null);
        await SeedCategoryAsync(host, "Phones", parent.Id);
        var handler = new CreateCategoryCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateCategoryCommand(new CreateCategoryRequest { Name = "Phones", ParentCategoryId = parent.Id }),
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
