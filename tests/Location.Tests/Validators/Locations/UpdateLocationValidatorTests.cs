using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Contracts.Common;
using StarterKit.Locations.Contracts.Locations;
using Xunit;

namespace Location.Tests.Validators.Locations;

/// <summary>
/// Covers both validator layers for <c>UpdateLocation</c>: the Contracts-level
/// <see cref="UpdateLocationRequestValidator"/> standalone, and the Api-level
/// <see cref="UpdateLocationCommandValidator"/> that validates <c>Id</c> directly and delegates the
/// model to it via <c>SetValidator</c>.
/// </summary>
public class UpdateLocationValidatorTests
{
    private static UpdateLocationRequest ValidRequest() =>
        new()
        {
            Name = "Main Store",
            Code = "STR-001",
            Status = LocationStatus.Active,
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new UpdateLocationRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new UpdateLocationRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new UpdateLocationRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenCodeIsEmpty(string? code)
    {
        var validator = new UpdateLocationRequestValidator();
        var request = ValidRequest() with { Code = code! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenCodeExceedsMaxLength()
    {
        var validator = new UpdateLocationRequestValidator();
        var request = ValidRequest() with { Code = new string('a', 51) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenStatusIsNotDefined()
    {
        var validator = new UpdateLocationRequestValidator();
        var request = ValidRequest() with { Status = (LocationStatus)999 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdAndModelAreValid()
    {
        var validator = new UpdateLocationCommandValidator();
        var command = new UpdateLocationCommand("loc-1", ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new UpdateLocationCommandValidator();
        var command = new UpdateLocationCommand("", ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelCodeIsEmpty()
    {
        var validator = new UpdateLocationCommandValidator();
        var command = new UpdateLocationCommand("loc-1", ValidRequest() with { Code = "" });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.Code);
    }
}
