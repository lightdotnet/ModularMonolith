using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Queries;
using StarterKit.Catalog.Api.Domain.Categories;
using Xunit;

namespace Catalog.Tests.Application.Categories.Queries;

public class GetCategoryTreeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoCategories()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new GetCategoryTreeQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetCategoryTreeQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldBuildMultiLevelTree()
    {
        // Arrange: root -> child, plus a second independent root.
        using var host = new CatalogTestHost();
        var rootA = Category.Create("Root A", null);
        var rootB = Category.Create("Root B", null);
        await host.Context.Categories.AddRangeAsync(rootA, rootB);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var child = Category.Create("Child", rootA.Id);
        await host.Context.Categories.AddAsync(child, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCategoryTreeQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetCategoryTreeQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        var rootANode = Assert.Single(result, x => x.Id == rootA.Id);
        var childNode = Assert.Single(rootANode.Children);
        Assert.Equal(child.Id, childNode.Id);
        var rootBNode = Assert.Single(result, x => x.Id == rootB.Id);
        Assert.Empty(rootBNode.Children);
    }
}
