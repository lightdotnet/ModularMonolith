using FluentValidation.TestHelper;
using StarterKit.Inventory.Api.Application.StockAdjustments.Commands;
using StarterKit.Inventory.Contracts.Stock;
using Xunit;

namespace Inventory.Tests.Validators.Stock;

/// <summary>Covers the unit-cost and quantity bounds of <see cref="RecordStockMovementRequestValidator"/>.</summary>
public class RecordStockMovementUnitCostValidatorTests
{
    private readonly RecordStockMovementRequestValidator _requestValidator = new();
    private readonly RecordStockMovementCommandValidator _commandValidator = new();

    private static RecordStockMovementRequest ValidRequest() =>
        new()
        {
            ProductId = 1,
            LocationId = "location-1",
            QuantityDelta = 5,
        };

    [Theory]
    [InlineData("0")]
    [InlineData("0.0001")]
    [InlineData("1000000000")]
    [InlineData("99.9999")]
    public void RequestValidator_ShouldNotHaveError_WhenUnitCostIsWithinTheBounds(string unitCost)
    {
        var request = ValidRequest();
        request.UnitCost = decimal.Parse(unitCost);

        _requestValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.UnitCost);
    }

    [Fact]
    public void RequestValidator_ShouldNotHaveError_WhenUnitCostIsOmitted()
    {
        _requestValidator.TestValidate(ValidRequest()).ShouldNotHaveValidationErrorFor(x => x.UnitCost);
    }

    [Fact]
    public void RequestValidator_ShouldHaveError_WhenUnitCostIsNegative()
    {
        var request = ValidRequest();
        request.UnitCost = -0.0001m;

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.UnitCost);
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

    [Theory]
    [InlineData(1_000_000_000)]
    [InlineData(-1_000_000_000)]
    public void RequestValidator_ShouldNotHaveError_WhenQuantityDeltaIsAtTheBound(int quantityDelta)
    {
        var request = ValidRequest();
        request.QuantityDelta = quantityDelta;

        _requestValidator.TestValidate(request).ShouldNotHaveValidationErrorFor(x => x.QuantityDelta);
    }

    [Theory]
    [InlineData(1_000_000_001)]
    [InlineData(-1_000_000_001)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void RequestValidator_ShouldHaveError_WhenQuantityDeltaIsBeyondTheBound(int quantityDelta)
    {
        var request = ValidRequest();
        request.QuantityDelta = quantityDelta;

        _requestValidator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.QuantityDelta);
    }

    [Fact]
    public void CommandValidator_ShouldHaveNestedModelError_WhenTheUnitCostIsTooLarge()
    {
        var request = ValidRequest();
        request.UnitCost = 1_000_000_001m;

        _commandValidator
            .TestValidate(new RecordStockMovementCommand(request, "user-1"))
            .ShouldHaveValidationErrorFor("Model.UnitCost");
    }
}
