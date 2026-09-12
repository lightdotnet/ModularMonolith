using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateAllFields_WhenRequestIsValid()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var categoryA = await SeedCategoryAsync(host, "Category A");
        var categoryB = await SeedCategoryAsync(host, "Category B");
        var product = await SeedProductAsync(host, categoryA.Id);
        var handler = new UpdateProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateProductCommand(
                product.Id,
                new UpdateProductRequest
                {
                    CategoryId = categoryB.Id,
                    Name = "Gadget",
                    Description = "Updated",
                    Price = 200m,
                    Currency = CurrencyConstants.Default,
                    VatRate = 20m,
                }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products.FindAsync([product.Id], TestContext.Current.CancellationToken);
        Assert.Equal(categoryB.Id, updated!.CategoryId);
        Assert.Equal("Gadget", updated.Name);
        Assert.Equal("Updated", updated.Description);
        Assert.Equal(200m, updated.Price.Amount);
        Assert.Equal(20m, updated.VatRate.Value);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpdateProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateProductCommand("missing", ValidRequest(category.Id)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id);
        var handler = new UpdateProductCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateProductCommand(product.Id, ValidRequest("missing")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static UpdateProductRequest ValidRequest(string categoryId) =>
        new()
        {
            CategoryId = categoryId,
            Name = "Gadget",
            Description = "Updated",
            Price = 200m,
            Currency = CurrencyConstants.Default,
            VatRate = 20m,
        };

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host, string name)
    {
        var category = Category.Create(name, null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }

    private static async Task<Product> SeedProductAsync(CatalogTestHost host, string categoryId)
    {
        var product = Product.Create(
            categoryId,
            "Widget",
            "A widget",
            new Sku("SKU-001"),
            new Money(100m, CurrencyConstants.Default),
            new VatPercentage(10m));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }
}
