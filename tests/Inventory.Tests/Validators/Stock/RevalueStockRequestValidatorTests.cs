using FluentValidation.TestHelper;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Validators.Stock;

/// <summary>Covers <see cref="RevalueStockRequestValidator"/> and the command validator that delegates to it.</summary>
public class RevalueStockRequestValidatorTests
{
    private readonly RevalueStockRequestValidator _requestValidator = new();
    private readonly RevalueStockCommandValidator _commandValidator = new();

    private static RevalueStockRequest ValidRequest() =>
        new()
        {
            ProductId = 1,
            LocationId = "location-1",
            UnitCost = 2.5m,
        };

    [Fact]
    public void RequestValidator_ShouldNotHaveErrors_WhenRequestIsValid()
    {
        _requestValidator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void RequestValidator_ShouldHaveError_WhenUnitCostIsNotPositive(string unitCost)
    {
        var request = ValidRequest();
        request.UnitCost = decimal.Parse(unitCost);

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.UnitCost);
    }

    [Theory]
    [InlineData("0.0001")]
    [InlineData("1000000000")]
    [InlineData("123456.1234")]
    public void RequestValidator_ShouldNotHaveError_WhenUnitCostIsWithinTheBounds(string unitCost)
    {
        var request = ValidRequest();
        request.UnitCost = decimal.Parse(unitCost);

        _requestValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.UnitCost);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenUnitCostExceedsOneBillion()
    {
        var request = ValidRequest();
        request.UnitCost = 1_000_000_000.0001m;

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.UnitCost);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenUnitCostHasMoreThanFourDecimals()
    {
        var request = ValidRequest();
        request.UnitCost = 1.00001m;

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.UnitCost);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenProductIdIsNotPositive()
    {
        var request = ValidRequest();
        request.ProductId = 0;

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void RequestValidator_ShouldHaveError_WhenLocationIdIsEmpty(string? locationId)
    {
        var request = ValidRequest();
        request.LocationId = locationId!;

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.LocationId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenLocationIdExceedsMaxLength()
    {
        var request = ValidRequest();
        request.LocationId = new string('a', 451);

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.LocationId);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenNoteExceedsMaxLength()
    {
        var request = ValidRequest();
        request.Note = new string('n', 501);

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void CommandValidator_ShouldNotHaveErrors_WhenModelIsValid()
    {
        _commandValidator
            .TestValidate(new RevalueStockCommand(ValidRequest(), "user-1"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CommandValidator_ShouldHaveError_WhenPerformedByUserIdIsEmpty(string? performedBy)
    {
        _commandValidator
            .TestValidate(new RevalueStockCommand(ValidRequest(), performedBy!))
            .ShouldHaveValidationErrorFor(x => x.PerformedByUserId);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenTheUnitCostIsZero()
    {
        var request = ValidRequest();
        request.UnitCost = 0m;

        _commandValidator
            .TestValidate(new RevalueStockCommand(request, "user-1"))
            .ShouldHaveValidationErrorFor("Model.UnitCost");
    }
}
