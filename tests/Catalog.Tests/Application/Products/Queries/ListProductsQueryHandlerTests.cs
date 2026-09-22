using Catalog.Tests.TestSupport;
using StarterKit.Catalog.Api.Application.Products.Queries;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Common;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Queries;

/// <summary>
/// <c>ListProductsQueryHandler</c> returns <see cref="StarterKit.Catalog.Contracts.Products.ProductDto"/>
/// wrapped in <c>Light.Contracts.PagedResult&lt;T&gt;</c>, which flows paging metadata through a nested
/// <c>Data</c> (<c>Paged&lt;T&gt;</c>) — same shape exercised by
/// <c>Organization.Tests.Application.Employees.Queries.SearchEmployeesQueryHandlerTests</c>.
/// </summary>
public class ListProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFilterByCategoryId()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var categoryA = await SeedCategoryAsync(host, "Category A");
        var categoryB = await SeedCategoryAsync(host, "Category B");
        await SeedProductAsync(host, categoryA.Id, "Widget A", "SKU-A");
        await SeedProductAsync(host, categoryB.Id, "Widget B", "SKU-B");
        var handler = new ListProductsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new ListProductsQuery(new ProductSearchRequest { CategoryId = categoryA.Id, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Widget A", Assert.Single(result.Data.Records).Name);
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var activeProduct = await SeedProductAsync(host, category.Id, "Active Widget", "SKU-A");
        var inactiveProduct = await SeedProductAsync(host, category.Id, "Inactive Widget", "SKU-B");
        inactiveProduct.Deactivate();
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ListProductsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new ListProductsQuery(new ProductSearchRequest { Status = ProductStatus.Inactive, PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal(inactiveProduct.Id, result.Data.Records.Single().Id);
        _ = activeProduct;
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchValue()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        await SeedProductAsync(host, category.Id, "Blue Widget", "SKU-A");
        await SeedProductAsync(host, category.Id, "Red Gadget", "SKU-B");
        var handler = new ListProductsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new ListProductsQuery(new ProductSearchRequest { SearchValue = "Widget", PageNumber = 1, PageSize = 10 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.TotalRecords);
        Assert.Equal("Blue Widget", result.Data.Records.Single().Name);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagingShape()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        await SeedProductAsync(host, category.Id, "Widget A", "SKU-A");
        await SeedProductAsync(host, category.Id, "Widget B", "SKU-B");
        await SeedProductAsync(host, category.Id, "Widget C", "SKU-C");
        var handler = new ListProductsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new ListProductsQuery(new ProductSearchRequest { PageNumber = 1, PageSize = 2 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, result.Data.PageNumber);
        Assert.Equal(2, result.Data.PageSize);
        Assert.Equal(3, result.Data.TotalRecords);
        Assert.Equal(2, result.Data.Records.Count());
    }

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host, string name)
    {
        var category = Category.Create(name, null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }

    private static async Task<Product> SeedProductAsync(CatalogTestHost host, string categoryId, string name, string sku)
    {
        var product = Product.Create(
            categoryId,
            name,
            null,
            new Sku(sku),
            new Money(100m, CurrencyConstants.Default),
            new VatPercentage(10m));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }
}
