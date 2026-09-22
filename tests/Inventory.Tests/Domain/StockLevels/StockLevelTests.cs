using Light.Exceptions;
using StarterKit.Inventory.Api.Domain.StockLevels;
using Xunit;

namespace Inventory.Tests.Domain.StockLevels;

public class StockLevelTests
{
    [Fact]
    public void Create_ShouldThrowValidationException_WhenProductIdIsNotPositive()
    {
        Assert.Throws<ValidationException>(() => StockLevel.Create(0, "location-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldThrowValidationException_WhenLocationIdIsEmpty(string? locationId)
    {
        Assert.Throws<ValidationException>(() => StockLevel.Create(1, locationId!));
    }

    [Fact]
    public void Create_ShouldStartAtZeroQuantityOnHand()
    {
        var level = StockLevel.Create(1, "location-1");

        Assert.Equal(0, level.QuantityOnHand);
    }

    [Fact]
    public void CanApply_ShouldReturnFalse_WhenDeltaWouldGoNegative()
    {
        var level = StockLevel.Create(1, "location-1");
        level.Apply(5);

        Assert.False(level.CanApply(-6));
    }

    [Fact]
    public void CanApply_ShouldReturnTrue_WhenDeltaKeepsQuantityAtOrAboveZero()
    {
        var level = StockLevel.Create(1, "location-1");
        level.Apply(5);

        Assert.True(level.CanApply(-5));
    }

    [Fact]
    public void Apply_ShouldIncreaseQuantityOnHand_WhenDeltaIsPositive()
    {
        var level = StockLevel.Create(1, "location-1");

        level.Apply(10);

        Assert.Equal(10, level.QuantityOnHand);
    }

    [Fact]
    public void Apply_ShouldAllowExactlyZeroingOut_WhenDeltaMatchesQuantityOnHand()
    {
        var level = StockLevel.Create(1, "location-1");
        level.Apply(5);

        level.Apply(-5);

        Assert.Equal(0, level.QuantityOnHand);
    }

    [Fact]
    public void Apply_ShouldThrowConflictException_WhenDeltaWouldGoNegative()
    {
        var level = StockLevel.Create(1, "location-1");
        level.Apply(5);

        Assert.Throws<StarterKit.Inventory.Contracts.Exceptions.InsufficientStockException>(() => level.Apply(-6));
    }

    [Fact]
    public void Apply_ShouldNotChangeQuantityOnHand_WhenItThrows()
    {
        var level = StockLevel.Create(1, "location-1");
        level.Apply(5);

        Assert.Throws<StarterKit.Inventory.Contracts.Exceptions.InsufficientStockException>(() => level.Apply(-6));

        Assert.Equal(5, level.QuantityOnHand);
    }
}
