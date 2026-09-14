using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Shared.Constants;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenRequestIsValid()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var handler = new CreateProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateProductCommand(ValidRequest(category.Id)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.Products.FindAsync([result.Data], TestContext.Current.CancellationToken);
        Assert.Equal("Widget", entity!.Name);
        Assert.Equal("SKU-001", entity.Sku!.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new CreateProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new CreateProductCommand(ValidRequest("missing")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenSkuAlreadyExists()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var handler = new CreateProductCommandHandler(host.Context);
        await handler.Handle(new CreateProductCommand(ValidRequest(category.Id)), TestContext.Current.CancellationToken);

        // Act
        var result = await handler.Handle(
            new CreateProductCommand(ValidRequest(category.Id) with { Name = "Another Widget" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static CreateProductRequest ValidRequest(string categoryId) =>
        new()
        {
            CategoryId = categoryId,
            Name = "Widget",
            Description = "A widget",
            Sku = "SKU-001",
            Price = 100m,
            Currency = CurrencyConstants.Default,
            VatRate = 10m,
        };

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host)
    {
        var category = Category.Create("Electronics", null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }
}
