using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Queries;
using StarterKit.Catalog.Api.Domain.Categories;
using Xunit;

namespace Catalog.Tests.Application.Categories.Queries;

public class GetCategoryChildrenQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnChildren_WhenParentHasChildren()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var parent = Category.Create("Parent", null);
        await host.Context.Categories.AddAsync(parent, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var child = Category.Create("Child", parent.Id);
        await host.Context.Categories.AddAsync(child, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCategoryChildrenQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetCategoryChildrenQuery(parent.Id), TestContext.Current.CancellationToken);

        // Assert
        var childDto = Assert.Single(result);
        Assert.Equal(child.Id, childDto.Id);
        Assert.Equal("Child", childDto.Name);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoChildren()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var parent = Category.Create("Parent", null);
        await host.Context.Categories.AddAsync(parent, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCategoryChildrenQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetCategoryChildrenQuery(parent.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }
}
