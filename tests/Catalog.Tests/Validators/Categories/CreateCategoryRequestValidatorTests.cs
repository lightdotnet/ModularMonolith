using FluentValidation.TestHelper;
using StarterKit.Catalog.Contracts.Categories;
using Xunit;

namespace Catalog.Tests.Validators.Categories;

public class CreateCategoryRequestValidatorTests
{
    private static CreateCategoryRequest ValidRequest() =>
        new()
        {
            ParentCategoryId = null,
            Name = "Electronics",
        };

    [Fact]
    public void ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new CreateCategoryRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new CreateCategoryRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new CreateCategoryRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenParentCategoryIdExceedsMaxLength()
    {
        var validator = new CreateCategoryRequestValidator();
        var request = ValidRequest() with { ParentCategoryId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ParentCategoryId);
    }

    [Fact]
    public void ShouldNotHaveError_WhenParentCategoryIdIsNull()
    {
        var validator = new CreateCategoryRequestValidator();
        var request = ValidRequest() with { ParentCategoryId = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ParentCategoryId);
    }
}
