using FluentValidation.TestHelper;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Shared.Constants;
using Xunit;

namespace Catalog.Tests.Validators.Products;

public class UpsertProductRequestValidatorTests
{
    private static UpsertProductRequest ValidRequest() =>
        new()
        {
            CategoryId = "category-1",
            Name = "Widget",
            Description = "A widget",
            Sku = "SKU-001",
            Price = 100m,
            Currency = CurrencyConstants.Default,
            VatRate = 10m,
        };

    [Fact]
    public void ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new UpsertProductRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenSkuIsNull()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Sku = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Sku);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldHaveError_WhenCategoryIdIsEmpty(string? categoryId)
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { CategoryId = categoryId! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Description = new string('a', 2001) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldHaveError_WhenSkuExceedsMaxLength()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Sku = new string('a', 101) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public void ShouldHaveError_WhenPriceIsNegative()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Price = -1m };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void ShouldNotHaveError_WhenPriceIsZero()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Price = 0m };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void ShouldNotHaveError_WhenCurrencyIsAnyThreeLetterCode()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Currency = "USD" };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("U1D")]
    public void ShouldHaveError_WhenCurrencyIsNotThreeLetters(string currency)
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Currency = currency };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldHaveError_WhenCurrencyIsEmpty(string? currency)
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Currency = currency! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ShouldHaveError_WhenVatRateIsOutOfRange(decimal vatRate)
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { VatRate = vatRate };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.VatRate);
    }

    [Fact]
    public void ShouldHaveError_WhenImageUrlIsEmpty()
    {
        var validator = new UpsertProductRequestValidator();
        var request = ValidRequest() with { Images = [new ProductImageDto { Url = "" }] };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Images[0].Url");
    }
}
