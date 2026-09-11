using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Contracts.Locations;
using Xunit;

namespace Location.Tests.Validators.Locations;

/// <summary>
/// Covers both validator layers for <c>CreateLocation</c>: the Contracts-level
/// <see cref="CreateLocationRequestValidator"/> standalone, and the Api-level
/// <see cref="CreateLocationCommandValidator"/> that delegates to it via <c>SetValidator</c>.
/// </summary>
public class CreateLocationValidatorTests
{
    private static CreateLocationRequest ValidRequest() =>
        new()
        {
            ParentLocationId = null,
            LocationTypeId = "STORE",
            Name = "Main Store",
            Code = "STR-001",
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new CreateLocationRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenLocationTypeIdIsEmpty(string? locationTypeId)
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { LocationTypeId = locationTypeId! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LocationTypeId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenLocationTypeIdExceedsMaxLength()
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { LocationTypeId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LocationTypeId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenCodeIsEmpty(string? code)
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { Code = code! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenCodeExceedsMaxLength()
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { Code = new string('a', 51) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenParentLocationIdExceedsMaxLength()
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { ParentLocationId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ParentLocationId);
    }

    [Fact]
    public void RequestValidator_ShouldNotHaveError_WhenParentLocationIdIsNull()
    {
        var validator = new CreateLocationRequestValidator();
        var request = ValidRequest() with { ParentLocationId = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ParentLocationId);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenModelIsValid()
    {
        var validator = new CreateLocationCommandValidator();
        var command = new CreateLocationCommand(ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelNameIsEmpty()
    {
        var validator = new CreateLocationCommandValidator();
        var command = new CreateLocationCommand(ValidRequest() with { Name = "" });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.Name);
    }
}
