using FluentValidation.TestHelper;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Validators.OrderTypes;

/// <summary>
/// Covers the Api-level <see cref="DeleteOrderTypeCommandValidator"/> — <c>DeleteOrderType</c> has no
/// Contracts-level request DTO/validator, the command only validates the route <c>Id</c>/<c>Category</c>.
/// </summary>
public class DeleteOrderTypeValidatorTests
{
    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdAndCategoryAreProvided()
    {
        var validator = new DeleteOrderTypeCommandValidator();
        var command = new DeleteOrderTypeCommand("SHIPPING", OrderTypeCategory.Fee);

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new DeleteOrderTypeCommandValidator();
        var command = new DeleteOrderTypeCommand("", OrderTypeCategory.Fee);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenCategoryIsNotDefined()
    {
        var validator = new DeleteOrderTypeCommandValidator();
        var command = new DeleteOrderTypeCommand("SHIPPING", (OrderTypeCategory)999);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }
}
