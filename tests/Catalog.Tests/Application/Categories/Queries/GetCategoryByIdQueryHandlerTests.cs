using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Categories.Queries;
using StarterKit.Catalog.Api.Domain.Categories;
using Xunit;

namespace Catalog.Tests.Application.Categories.Queries;

public class GetCategoryByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCategory_WhenFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = Category.Create("Electronics", null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCategoryByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetCategoryByIdQuery(category.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Electronics", result.Data.Name);
        Assert.Null(result.Data.ParentCategoryId);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new GetCategoryByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetCategoryByIdQuery("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
