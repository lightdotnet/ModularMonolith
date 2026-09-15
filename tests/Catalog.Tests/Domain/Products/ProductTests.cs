using Light.Exceptions;
using StarterKit.Catalog.Api.Domain.Products;
using StarterKit.Catalog.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Catalog.Tests.Domain.Products;

public class ProductTests
{
    private static Sku ValidSku(string value = "SKU-001") => new(value);

    private static Money ValidPrice(decimal amount = 100m) => new(amount, CurrencyConstants.Default);

    private static VatPercentage ValidVat(decimal value = 10m) => new(value);

    [Fact]
    public void Create_ShouldSetAllFields()
    {
        // Arrange
        var sku = ValidSku();
        var price = ValidPrice();
        var vat = ValidVat();

        // Act
        var product = Product.Create("category-1", "Widget", "A widget", sku, price, vat);

        // Assert
        Assert.Equal("category-1", product.CategoryId);
        Assert.Equal("Widget", product.Name);
        Assert.Equal("A widget", product.Description);
        Assert.Same(sku, product.Sku);
        Assert.Same(price, product.Price);
        Assert.Same(vat, product.VatRate);
        Assert.Equal(ProductStatus.Active, product.Status);
        Assert.Empty(product.Images);
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenSkuIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => Product.Create("category-1", "Widget", null, null!, ValidPrice(), ValidVat()));
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenPriceIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => Product.Create("category-1", "Widget", null, ValidSku(), null!, ValidVat()));
    }

    [Fact]
    public void Create_ShouldThrowArgumentNullException_WhenVatRateIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), null!));
    }

    [Fact]
    public void Rename_ShouldMutateName()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act
        product.Rename("Gadget");

        // Assert
        Assert.Equal("Gadget", product.Name);
    }

    [Fact]
    public void UpdateDescription_ShouldMutateDescription()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act
        product.UpdateDescription("Updated description");

        // Assert
        Assert.Equal("Updated description", product.Description);
    }

    [Fact]
    public void Recategorize_ShouldMutateCategoryId()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act
        product.Recategorize("category-2");

        // Assert
        Assert.Equal("category-2", product.CategoryId);
    }

    [Fact]
    public void Reprice_ShouldMutatePriceInPlace()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(100m), ValidVat());
        var originalPrice = product.Price;

        // Act
        product.Reprice(new Money(200m, CurrencyConstants.Default));

        // Assert
        Assert.Same(originalPrice, product.Price);
        Assert.Equal(200m, product.Price.Amount);
        Assert.Equal(CurrencyConstants.Default, product.Price.Currency);
    }

    [Fact]
    public void Reprice_ShouldThrowArgumentNullException_WhenPriceIsNull()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => product.Reprice(null!));
    }

    [Fact]
    public void UpdateVatRate_ShouldMutateVatRateInPlace()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat(10m));
        var originalVatRate = product.VatRate;

        // Act
        product.UpdateVatRate(new VatPercentage(20m));

        // Assert
        Assert.Same(originalVatRate, product.VatRate);
        Assert.Equal(20m, product.VatRate.Value);
    }

    [Fact]
    public void UpdateVatRate_ShouldThrowArgumentNullException_WhenVatRateIsNull()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => product.UpdateVatRate(null!));
    }

    [Fact]
    public void AddImage_ShouldAppendToImages()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());
        var image = new ProductImageUrl("https://example.com/a.png");

        // Act
        product.AddImage(image);

        // Assert
        Assert.Same(image, Assert.Single(product.Images));
    }

    [Fact]
    public void RemoveImage_ShouldRemoveByUrl()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());
        product.AddImage(new ProductImageUrl("https://example.com/a.png"));

        // Act
        product.RemoveImage("https://example.com/a.png");

        // Assert
        Assert.Empty(product.Images);
    }

    [Fact]
    public void RemoveImage_ShouldNotThrow_WhenUrlIsNotPresent()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());
        product.AddImage(new ProductImageUrl("https://example.com/a.png"));

        // Act
        product.RemoveImage("https://example.com/missing.png");

        // Assert
        Assert.Single(product.Images);
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());
        product.Deactivate();

        // Act
        product.Activate();

        // Assert
        Assert.Equal(ProductStatus.Active, product.Status);
    }

    [Fact]
    public void Deactivate_ShouldSetStatusToInactive()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act
        product.Deactivate();

        // Assert
        Assert.Equal(ProductStatus.Inactive, product.Status);
    }

    [Fact]
    public void ClearSku_ShouldSetSkuToNull()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act
        product.UpdateSku(null);

        // Assert
        Assert.Null(product.Sku);
    }

    [Fact]
    public void ClearSku_ShouldNotThrow_WhenSkuIsAlreadyNull()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());
        product.UpdateSku(null);

        // Act
        product.UpdateSku(null);

        // Assert
        Assert.Null(product.Sku);
    }

    [Fact]
    public void Delete_ShouldThrowConflictException_WhenProductIsActive()
    {
        // Arrange
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());

        // Act & Assert
        Assert.Throws<ConflictException>(() => product.Delete());
    }

    [Fact]
    public void Delete_ShouldNotThrow_WhenProductIsInactive()
    {
        // Arrange — Delete() itself only asserts the guard; the actual soft-delete is triggered by
        // the caller (DeleteProductCommandHandler) via context.Products.Remove(entity) afterward.
        var product = Product.Create("category-1", "Widget", null, ValidSku(), ValidPrice(), ValidVat());
        product.Deactivate();

        // Act & Assert
        var exception = Record.Exception(() => product.Delete());
        Assert.Null(exception);
    }
}
