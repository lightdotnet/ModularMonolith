using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.Locations.Commands;
using Xunit;

namespace Location.Tests.Validators.Locations;

/// <summary>
/// Covers the Api-level <see cref="DeleteLocationCommandValidator"/> — <c>DeleteLocation</c> has no
/// Contracts-level request DTO/validator, the command only validates the route <c>Id</c>.
/// </summary>
public class DeleteLocationValidatorTests
{
    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdIsProvided()
    {
        var validator = new DeleteLocationCommandValidator();
        var command = new DeleteLocationCommand("loc-1");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new DeleteLocationCommandValidator();
        var command = new DeleteLocationCommand("");

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
