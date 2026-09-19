using FluentValidation.TestHelper;
using StarterKit.Orders.Api.Application.OrderTypes.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.OrderTypes;
using Xunit;

namespace Orders.Tests.Validators.OrderTypes;

/// <summary>
/// Covers both validator layers for <c>CreateOrderType</c>: the Contracts-level
/// <see cref="CreateOrderTypeRequestValidator"/> standalone, and the Api-level
/// <see cref="CreateOrderTypeCommandValidator"/> that delegates to it via <c>SetValidator</c>.
/// </summary>
public class CreateOrderTypeValidatorTests
{
    private static CreateOrderTypeRequest ValidRequest() =>
        new()
        {
            Id = "SHIPPING",
            Category = OrderTypeCategory.Fee,
            Name = "Shipping",
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        var validator = new CreateOrderTypeRequestValidator();

        var result = validator.TestValidate(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenIdIsEmpty(string? id)
    {
        var validator = new CreateOrderTypeRequestValidator();
        var request = ValidRequest() with { Id = id! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenIdExceedsMaxLength()
    {
        var validator = new CreateOrderTypeRequestValidator();
        var request = ValidRequest() with { Id = new string('a', 451) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenCategoryIsNotDefined()
    {
        var validator = new CreateOrderTypeRequestValidator();
        var request = ValidRequest() with { Category = (OrderTypeCategory)999 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenNameIsEmpty(string? name)
    {
        var validator = new CreateOrderTypeRequestValidator();
        var request = ValidRequest() with { Name = name! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var validator = new CreateOrderTypeRequestValidator();
        var request = ValidRequest() with { Name = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenModelIsValid()
    {
        var validator = new CreateOrderTypeCommandValidator();
        var command = new CreateOrderTypeCommand(ValidRequest());

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenModelIdIsEmpty()
    {
        var validator = new CreateOrderTypeCommandValidator();
        var command = new CreateOrderTypeCommand(ValidRequest() with { Id = "" });

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Model.Id);
    }
}
