using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.Locations.Commands;
using StarterKit.Locations.Contracts.Locations;
using Xunit;

namespace Location.Tests.Validators.Locations;

/// <summary>
/// Covers both validator layers for <c>MoveLocation</c>: the Contracts-level
/// <see cref="MoveLocationRequestValidator"/> standalone, and the Api-level
/// <see cref="MoveLocationCommandValidator"/> that validates <c>Id</c> directly and delegates the
/// model to it via <c>SetValidator</c>.
/// </summary>
public class MoveLocationValidatorTests
{
    private static MoveLocationRequest ValidRequest() =>
        new() { NewParentLocationId = "loc-parent" };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new MoveLocationRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RequestValidator_ShouldNotHaveError_WhenNewParentLocationIdIsNull()
    {
        var validator = new MoveLocationRequestValidator();
        var request = ValidRequest() with { NewParentLocationId = null };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.NewParentLocationId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNewParentLocationIdExceedsMaxLength()
    {
        var validator = new MoveLocationRequestValidator();
        var request = ValidRequest() with { NewParentLocationId = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.NewParentLocationId);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdAndModelAreValid()
    {
        var validator = new MoveLocationCommandValidator();
        var command = new MoveLocationCommand("loc-1", ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new MoveLocationCommandValidator();
        var command = new MoveLocationCommand("", ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelNewParentLocationIdExceedsMaxLength()
    {
        var validator = new MoveLocationCommandValidator();
        var command = new MoveLocationCommand("loc-1", ValidRequest() with { NewParentLocationId = new string('a', 451) });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.NewParentLocationId);
    }
}
