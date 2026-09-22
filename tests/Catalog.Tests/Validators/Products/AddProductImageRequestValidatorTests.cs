using FluentValidation.TestHelper;
using StarterKit.Catalog.Contracts.Products;
using Xunit;

namespace Catalog.Tests.Validators.Products;

public class AddProductImageRequestValidatorTests
{
    [Fact]
    public void ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new AddProductImageRequestValidator();
        var request = new AddProductImageRequest { Url = "https://example.com/a.png", SortOrder = 1 };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldHaveError_WhenUrlIsEmpty(string? url)
    {
        var validator = new AddProductImageRequestValidator();
        var request = new AddProductImageRequest { Url = url! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public void ShouldHaveError_WhenUrlExceedsMaxLength()
    {
        var validator = new AddProductImageRequestValidator();
        var request = new AddProductImageRequest { Url = new string('a', 2049) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public void ShouldNotHaveError_WhenSortOrderIsNull()
    {
        var validator = new AddProductImageRequestValidator();
        var request = new AddProductImageRequest { Url = "https://example.com/a.png", SortOrder = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }
}
