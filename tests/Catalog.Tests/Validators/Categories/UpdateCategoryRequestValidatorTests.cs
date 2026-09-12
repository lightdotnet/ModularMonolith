using FluentValidation.TestHelper;
using StarterKit.Catalog.Contracts.Categories;
using Xunit;

namespace Catalog.Tests.Validators.Categories;

public class UpdateCategoryRequestValidatorTests
{
    private static UpdateCategoryRequest ValidRequest() => new() { Name = "Electronics" };

    [Fact]
    public void ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new UpdateCategoryRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new UpdateCategoryRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new UpdateCategoryRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
