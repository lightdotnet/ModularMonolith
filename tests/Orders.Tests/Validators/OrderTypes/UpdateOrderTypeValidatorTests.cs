using FluentValidation.TestHelper;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.OrderTypes;
using Xunit;

namespace Orders.Tests.Validators.OrderTypes;

/// <summary>
/// Covers both validator layers for <c>UpdateOrderType</c>: the Contracts-level
/// <see cref="UpdateOrderTypeRequestValidator"/> standalone, and the Api-level
/// <see cref="UpdateOrderTypeCommandValidator"/> that validates <c>Id</c>/<c>Category</c> directly and
/// delegates the model to it via <c>SetValidator</c>.
/// </summary>
public class UpdateOrderTypeValidatorTests
{
    private static UpdateOrderTypeRequest ValidRequest() =>
        new()
        {
            Name = "Shipping",
            Status = OrderTypeStatus.Active,
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new UpdateOrderTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new UpdateOrderTypeRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new UpdateOrderTypeRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenStatusIsNotDefined()
    {
        var validator = new UpdateOrderTypeRequestValidator();
        var request = ValidRequest() with { Status = (OrderTypeStatus)999 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenIdAndModelAreValid()
    {
        var validator = new UpdateOrderTypeCommandValidator();
        var command = new UpdateOrderTypeCommand("SHIPPING", OrderTypeCategory.Fee, ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenIdIsEmpty()
    {
        var validator = new UpdateOrderTypeCommandValidator();
        var command = new UpdateOrderTypeCommand("", OrderTypeCategory.Fee, ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CommandValidator_ShouldHaveError_WhenCategoryIsNotDefined()
    {
        var validator = new UpdateOrderTypeCommandValidator();
        var command = new UpdateOrderTypeCommand("SHIPPING", (OrderTypeCategory)999, ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelNameIsEmpty()
    {
        var validator = new UpdateOrderTypeCommandValidator();
        var command = new UpdateOrderTypeCommand("SHIPPING", OrderTypeCategory.Fee, ValidRequest() with { Name = "" });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.Name);
    }
}
