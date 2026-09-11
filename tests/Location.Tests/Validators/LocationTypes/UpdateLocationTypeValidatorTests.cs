using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.LocationTypes;
using Xunit;

namespace Location.Tests.Validators.LocationTypes;

/// <summary>
/// Covers both validator layers for <c>UpdateLocationType</c>: the Contracts-level
/// <see cref="UpdateLocationTypeRequestValidator"/> standalone, and the Api-level
/// <see cref="UpdateLocationTypeCommandValidator"/> that validates <c>Id</c> directly and delegates
/// the model to it via <c>SetValidator</c>.
/// </summary>
public class UpdateLocationTypeValidatorTests
{
    private static UpdateLocationTypeRequest ValidRequest() =>
        new()
        {
            Name = "Store",
            AllowedParentTypeId = null,
            CanHaveChildren = true,
            Status = LocationTypeStatus.Active,
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new UpdateLocationTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new UpdateLocationTypeRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new UpdateLocationTypeRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenAllowedParentTypeIdExceedsMaxLength()
    {
        var validator = new UpdateLocationTypeRequestValidator();
        var request = ValidRequest() with { AllowedParentTypeId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AllowedParentTypeId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenStatusIsNotDefined()
    {
        var validator = new UpdateLocationTypeRequestValidator();
        var request = ValidRequest() with { Status = (LocationTypeStatus)999 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdAndModelAreValid()
    {
        var validator = new UpdateLocationTypeCommandValidator();
        var command = new UpdateLocationTypeCommand("STORE", ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new UpdateLocationTypeCommandValidator();
        var command = new UpdateLocationTypeCommand("", ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelNameIsEmpty()
    {
        var validator = new UpdateLocationTypeCommandValidator();
        var command = new UpdateLocationTypeCommand("STORE", ValidRequest() with { Name = "" });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.Name);
    }
}
