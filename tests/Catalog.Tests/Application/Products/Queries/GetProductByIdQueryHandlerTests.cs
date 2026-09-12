using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Products.Queries;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Queries;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnProduct_WhenFound()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host);
        var product = Product.Create(
            category.Id,
            "Widget",
            "A widget",
            new Sku("SKU-001"),
            new Money(100m, CurrencyConstants.Default),
            new VatPercentage(10m));
        product.AddImage(new ProductImageUrl("https://example.com/b.png", 2));
        product.AddImage(new ProductImageUrl("https://example.com/a.png", 1));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetProductByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Data;
        Assert.Equal(category.Id, dto.CategoryId);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal("A widget", dto.Description);
        Assert.Equal("SKU-001", dto.Sku);
        Assert.Equal(100m, dto.Price);
        Assert.Equal(CurrencyConstants.Default, dto.Currency);
        Assert.Equal(10m, dto.VatRate);
        Assert.Equal(2, dto.Images.Count);
        Assert.Equal("https://example.com/a.png", dto.Images[0].Url);
        Assert.Equal("https://example.com/b.png", dto.Images[1].Url);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new GetProductByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetProductByIdQuery("missing"), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host)
    {
        var category = Category.Create("Electronics", null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }
}
