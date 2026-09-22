using Light.Exceptions;
using StarterKit.Inventory.Api.Domain.StockLevels;
using Xunit;

namespace Inventory.Tests.Domain.StockLevels;

/// <summary>Covers the valuation (moving-average cost) behavior of <see cref="StockLevel"/>.</summary>
public class StockLevelValuationTests
{
    private const string LocationId = "location-1";

    private static StockLevel LevelWith(
        int quantity,
        decimal value)
    {
        var level = StockLevel.Create(1, LocationId);
        level.Apply(quantity, value);

        return level;
    }

    [Theory]
    [InlineData(3, "1.23456", "3.7037")]
    [InlineData(10, "2", "20")]
    [InlineData(1, "0.00005", "0.0001")]
    [InlineData(1, "0.00004", "0.0000")]
    [InlineData(7, "0", "0")]
    public void ValueOfInbound_ShouldRoundToFourDecimals_AwayFromZero(
        int quantity,
        string unitCost,
        string expected)
    {
        var value = StockLevel.ValueOfInbound(quantity, decimal.Parse(unitCost));

        Assert.Equal(decimal.Parse(expected), value);
    }

    [Fact]
    public void ValueOfOutbound_ShouldBeProportionalAndRounded_WhenIssuingPartOfTheStock()
    {
        var level = LevelWith(3, 10m);

        Assert.Equal(3.3333m, level.ValueOfOutbound(1));
        Assert.Equal(6.6667m, level.ValueOfOutbound(2));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void ValueOfOutbound_ShouldReturnTheWholeRemainingValue_WhenTheLevelIsEmptied(int quantity)
    {
        var level = LevelWith(3, 10m);

        Assert.Equal(10m, level.ValueOfOutbound(quantity));
    }

    [Fact]
    public void ValueOfOutbound_ShouldReturnZero_WhenNothingIsOnHand()
    {
        var level = StockLevel.Create(1, LocationId);

        Assert.Equal(0m, level.ValueOfOutbound(4));
    }

    [Fact]
    public void ValueOfOutbound_ShouldNotLeaveResidue_WhenIssuedInSteps()
    {
        // Arrange — 3 units worth 10.0000, issued one by one: the last step must take the exact rest.
        var level = LevelWith(3, 10m);
        var removed = 0m;

        // Act
        for (var i = 0; i < 3; i++)
        {
            var value = level.ValueOfOutbound(1);
            level.Apply(-1, -value);
            removed += value;
        }

        // Assert
        Assert.Equal(0, level.QuantityOnHand);
        Assert.Equal(0m, level.TotalValueBase);
        Assert.Equal(10m, removed);
    }

    [Fact]
    public void AverageCostBase_ShouldBeDerivedFromTotalValueAndQuantity()
    {
        var level = LevelWith(3, 10m);

        Assert.Equal(3.3333m, level.AverageCostBase);
    }

    [Fact]
    public void AverageCostBase_ShouldBeZero_WhenNothingIsOnHand()
    {
        var level = StockLevel.Create(1, LocationId);

        Assert.Equal(0m, level.AverageCostBase);
    }

    [Theory]
    [InlineData("10", 4, "2.5")]
    [InlineData("0", 0, "0")]
    [InlineData("10", 3, "3.3333")]
    public void UnitCostOf_ShouldSpreadTheValueOverTheQuantity(
        string value,
        int quantity,
        string expected)
    {
        Assert.Equal(decimal.Parse(expected), StockLevel.UnitCostOf(decimal.Parse(value), quantity));
    }

    [Theory]
    [InlineData("4", "2")]
    [InlineData("2", "-4")]
    [InlineData("2.0000", "-4")]
    [InlineData("3.3333", "-0.0001")]
    public void ValueChangeForRevaluation_ShouldBeTheDifferenceToTheRepricedStock(
        string newUnitCost,
        string expectedChange)
    {
        // 3 units worth 10 today: repricing at 4 makes 12 (+2), at 2 makes 6 (-4).
        var level = LevelWith(3, 10m);

        Assert.Equal(decimal.Parse(expectedChange), level.ValueChangeForRevaluation(decimal.Parse(newUnitCost)));
    }

    [Fact]
    public void ValueChangeForRevaluation_ShouldBeZero_WhenNothingIsOnHand()
    {
        var level = StockLevel.Create(1, LocationId);

        Assert.Equal(0m, level.ValueChangeForRevaluation(5m));
    }

    [Fact]
    public void Apply_ShouldChangeQuantityAndValueTogether()
    {
        var level = LevelWith(5, 10m);

        level.Apply(3, 9m);

        Assert.Equal(8, level.QuantityOnHand);
        Assert.Equal(19m, level.TotalValueBase);
    }

    [Fact]
    public void Apply_ShouldAllowAQuantityNeutralValueChange()
    {
        var level = LevelWith(5, 10m);

        level.Apply(0, 5m);

        Assert.Equal(5, level.QuantityOnHand);
        Assert.Equal(15m, level.TotalValueBase);
    }

    [Fact]
    public void Apply_ShouldThrowValidationException_AndChangeNothing_WhenValueWouldBecomeNegative()
    {
        var level = LevelWith(5, 10m);

        Assert.Throws<ValidationException>(() => level.Apply(-1, -10.0001m));

        Assert.Equal(5, level.QuantityOnHand);
        Assert.Equal(10m, level.TotalValueBase);
    }

    [Fact]
    public void Apply_ShouldThrowValidationException_WhenEmptyingTheLevelLeavesValueBehind()
    {
        var level = LevelWith(5, 10m);

        Assert.Throws<ValidationException>(() => level.Apply(-5, -9m));

        Assert.Equal(5, level.QuantityOnHand);
        Assert.Equal(10m, level.TotalValueBase);
    }

    [Fact]
    public void Apply_ShouldAllowEmptyingTheLevel_WhenAllValueLeavesWithIt()
    {
        var level = LevelWith(5, 10m);

        level.Apply(-5, -10m);

        Assert.Equal(0, level.QuantityOnHand);
        Assert.Equal(0m, level.TotalValueBase);
    }

    [Fact]
    public void Apply_ShouldThrowValidationException_WhenAnEmptyLevelWouldGainValueWithoutQuantity()
    {
        var level = StockLevel.Create(1, LocationId);

        Assert.Throws<ValidationException>(() => level.Apply(0, 5m));

        Assert.Equal(0m, level.TotalValueBase);
    }

    [Fact]
    public void Apply_ShouldThrowConflictException_WhenTheQuantityWouldOverflowAnInt()
    {
        var level = StockLevel.Create(1, LocationId);
        level.Apply(int.MaxValue);

        Assert.False(level.CanApply(1));
        Assert.Throws<StarterKit.Inventory.Contracts.Exceptions.InsufficientStockException>(() => level.Apply(1));
        Assert.Equal(int.MaxValue, level.QuantityOnHand);
    }

    [Fact]
    public void Apply_ShouldThrowConflictException_WhenTheDeltaIsIntMinValue()
    {
        var level = LevelWith(5, 10m);

        Assert.False(level.CanApply(int.MinValue));
        Assert.Throws<StarterKit.Inventory.Contracts.Exceptions.InsufficientStockException>(() => level.Apply(int.MinValue));
        Assert.Equal(5, level.QuantityOnHand);
    }
}
