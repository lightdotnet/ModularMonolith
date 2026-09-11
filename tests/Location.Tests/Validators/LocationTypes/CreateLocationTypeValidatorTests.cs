using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using StarterKit.Locations.Contracts.LocationTypes;
using Xunit;

namespace Location.Tests.Validators.LocationTypes;

/// <summary>
/// Covers both validator layers for <c>CreateLocationType</c>: the Contracts-level
/// <see cref="CreateLocationTypeRequestValidator"/> standalone, and the Api-level
/// <see cref="CreateLocationTypeCommandValidator"/> that delegates to it via <c>SetValidator</c>.
/// </summary>
public class CreateLocationTypeValidatorTests
{
    private static CreateLocationTypeRequest ValidRequest() =>
        new()
        {
            Id = "STORE",
            Name = "Store",
            AllowedParentTypeId = null,
            CanHaveChildren = true,
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new CreateLocationTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenIdIsEmpty(string? id)
    {
        var validator = new CreateLocationTypeRequestValidator();
        var request = ValidRequest() with { Id = id! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenIdExceedsMaxLength()
    {
        var validator = new CreateLocationTypeRequestValidator();
        var request = ValidRequest() with { Id = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new CreateLocationTypeRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new CreateLocationTypeRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldNotHaveError_WhenAllowedParentTypeIdIsNull()
    {
        var validator = new CreateLocationTypeRequestValidator();
        var request = ValidRequest() with { AllowedParentTypeId = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.AllowedParentTypeId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenAllowedParentTypeIdExceedsMaxLength()
    {
        var validator = new CreateLocationTypeRequestValidator();
        var request = ValidRequest() with { AllowedParentTypeId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AllowedParentTypeId);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenModelIsValid()
    {
        var validator = new CreateLocationTypeCommandValidator();
        var command = new CreateLocationTypeCommand(ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelIdIsEmpty()
    {
        var validator = new CreateLocationTypeCommandValidator();
        var command = new CreateLocationTypeCommand(ValidRequest() with { Id = "" });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.Id);
    }
}
