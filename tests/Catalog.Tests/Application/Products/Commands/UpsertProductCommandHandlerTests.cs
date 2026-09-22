using Catalog.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using StarterKit.Catalog.Api.Application.Products.Commands;
using StarterKit.Catalog.Api.Domain.Categories;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Currencies.Contracts.Services;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Application.Products.Commands;

public class UpsertProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateProduct_WhenRequestIsValid()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, ValidRequest(category.Id)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.Products.FindAsync([result.Data], TestContext.Current.CancellationToken);
        Assert.Equal("Widget", entity!.Name);
        Assert.Equal("SKU-001", entity.Sku!.Value);
    }

    [Fact]
    public async Task Handle_ShouldPersistImages_WhenCreatingWithImages()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());
        var request = ValidRequest(category.Id) with
        {
            Images =
            [
                new ProductImageDto { Url = "https://example.com/a.png", SortOrder = 1 },
                new ProductImageDto { Url = "https://example.com/b.png", SortOrder = 2 },
            ],
        };

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, request),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == result.Data, TestContext.Current.CancellationToken);
        Assert.Equal(2, entity.Images.Count);
        Assert.Contains(entity.Images, x => x.Url == "https://example.com/a.png");
        Assert.Contains(entity.Images, x => x.Url == "https://example.com/b.png");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCreatingAndCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, ValidRequest("missing")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenCreatingAndSkuAlreadyExists()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());
        await handler.Handle(
            new UpsertProductCommand(null, ValidRequest(category.Id)),
            TestContext.Current.CancellationToken);

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, ValidRequest(category.Id) with { Name = "Another Widget" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAllFields_WhenRequestIsValid()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var categoryA = await SeedCategoryAsync(host, "Category A");
        var categoryB = await SeedCategoryAsync(host, "Category B");
        var product = await SeedProductAsync(host, categoryA.Id);
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(
                product.Id,
                new UpsertProductRequest
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
    public async Task Handle_ShouldReplaceImages_WhenRequestContainsDifferentImages()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id);
        product.AddImage(new ProductImageUrl("https://example.com/old.png"));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        host.Context.ChangeTracker.Clear();
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());
        var request = ValidRequest(category.Id) with
        {
            Images =
            [
                new ProductImageDto { Url = "https://example.com/new-1.png", SortOrder = 1 },
                new ProductImageDto { Url = "https://example.com/new-2.png", SortOrder = 2 },
            ],
        };

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(product.Id, request),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, updated.Images.Count);
        Assert.DoesNotContain(updated.Images, x => x.Url == "https://example.com/old.png");
        Assert.Contains(updated.Images, x => x.Url == "https://example.com/new-1.png");
        Assert.Contains(updated.Images, x => x.Url == "https://example.com/new-2.png");
    }

    [Fact]
    public async Task Handle_ShouldClearPersistedImages_WhenRequestContainsNoImages()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id);
        product.AddImage(new ProductImageUrl("https://example.com/a.png"));
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        host.Context.ChangeTracker.Clear();
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(product.Id, ValidRequest(category.Id)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Empty(updated.Images);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUpdatingAndProductDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(999, ValidRequest(category.Id)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUpdatingAndCategoryDoesNotExist()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id);
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(product.Id, ValidRequest("missing")),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductInForeignCurrency_WhenCurrencyIsActive()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, ValidRequest(category.Id) with { Currency = "USD" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var entity = await host.Context.Products.FindAsync([result.Data], TestContext.Current.CancellationToken);
        Assert.Equal("USD", entity!.Price.Currency);
    }

    [Fact]
    public async Task Handle_ShouldRepriceInForeignCurrency_WhenUpdatingWithActiveCurrency()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id);
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(product.Id, ValidRequest(category.Id) with { Currency = "USD" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products.SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Equal("USD", updated.Price.Currency);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCurrencyIsUnknown()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, ValidRequest(category.Id) with { Currency = "ZZZ" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("ZZZ", result.Message);
        Assert.False(await host.Context.Products.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCurrencyIsInactive()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(null, ValidRequest(category.Id) with { Currency = "EUR" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(await host.Context.Products.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldAllowEditing_WhenTheStoredCurrencyWasDeactivatedButIsUnchanged()
    {
        // Arrange: EUR is inactive in the Currency double, yet the product already carries it.
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id, "EUR");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(product.Id, ValidRequest(category.Id) with { Name = "Renamed", Currency = "EUR" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await host.Context.Products.SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Renamed", updated.Name);
        Assert.Equal("EUR", updated.Price.Currency);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenChangingAnExistingProductToAnInactiveCurrency()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var product = await SeedProductAsync(host, category.Id);
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(product.Id, ValidRequest(category.Id) with { Currency = "EUR" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        host.Context.ChangeTracker.Clear();
        var stored = await host.Context.Products.SingleAsync(x => x.Id == product.Id, TestContext.Current.CancellationToken);
        Assert.Equal("VND", stored.Price.Currency);
    }

    [Fact]
    public async Task Handle_ShouldReportTheMissingProduct_BeforeCheckingTheCurrency()
    {
        // Arrange
        using var host = new CatalogTestHost();
        var category = await SeedCategoryAsync(host, "Electronics");
        var handler = new UpsertProductCommandHandler(host.Context, CurrencyService());

        // Act
        var result = await handler.Handle(
            new UpsertProductCommand(999, ValidRequest(category.Id) with { Currency = "ZZZ" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Product 999", result.Message);
    }

    /// <summary>Currency module double: VND and USD are active, EUR is inactive, anything else is unknown.</summary>
    private static ICurrencyService CurrencyService()
    {
        var currencyService = new Mock<ICurrencyService>();

        currencyService
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) => code.ToUpperInvariant() switch
            {
                "VND" => new CurrencyInfoDto("VND", 0, true, true),
                "USD" => new CurrencyInfoDto("USD", 2, true, false),
                "EUR" => new CurrencyInfoDto("EUR", 2, false, false),
                _ => null,
            });

        return currencyService.Object;
    }

    private static UpsertProductRequest ValidRequest(string categoryId) =>
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

    private static async Task<Category> SeedCategoryAsync(CatalogTestHost host, string name)
    {
        var category = Category.Create(name, null);
        await host.Context.Categories.AddAsync(category, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category;
    }

    private static async Task<Product> SeedProductAsync(
        CatalogTestHost host,
        string categoryId,
        string currency = CurrencyConstants.Default)
    {
        var product = Product.Create(
            categoryId,
            "Widget",
            "A widget",
            new Sku("SKU-001"),
            new Money(100m, currency),
            new VatPercentage(10m));
        await host.Context.Products.AddAsync(product, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return product;
    }
}
