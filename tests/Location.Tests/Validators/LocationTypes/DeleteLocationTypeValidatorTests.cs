using FluentValidation.TestHelper;
using StarterKit.Locations.Api.Application.LocationTypes.Commands;
using Xunit;

namespace Location.Tests.Validators.LocationTypes;

/// <summary>
/// Covers the Api-level <see cref="DeleteLocationTypeCommandValidator"/> — <c>DeleteLocationType</c>
/// has no Contracts-level request DTO/validator, the command only validates the route <c>Id</c>.
/// </summary>
public class DeleteLocationTypeValidatorTests
{
    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdIsProvided()
    {
        var validator = new DeleteLocationTypeCommandValidator();
        var command = new DeleteLocationTypeCommand("STORE");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new DeleteLocationTypeCommandValidator();
        var command = new DeleteLocationTypeCommand("");

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
