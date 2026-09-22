using FluentValidation.TestHelper;
using StarterKit.Catalog.Contracts.Categories;
using Xunit;

namespace Catalog.Tests.Validators.Categories;

public class MoveCategoryRequestValidatorTests
{
    [Fact]
    public void ShouldNotHaveErrors_WhenNewParentCategoryIdIsNull()
    {
        var validator = new MoveCategoryRequestValidator();
        var request = new MoveCategoryRequest { NewParentCategoryId = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveErrors_WhenNewParentCategoryIdIsProvided()
    {
        var validator = new MoveCategoryRequestValidator();
        var request = new MoveCategoryRequest { NewParentCategoryId = "parent-1" };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenNewParentCategoryIdExceedsMaxLength()
    {
        var validator = new MoveCategoryRequestValidator();
        var request = new MoveCategoryRequest { NewParentCategoryId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewParentCategoryId);
    }
}
