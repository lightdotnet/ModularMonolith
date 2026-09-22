using Light.Exceptions;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Framework.Tests.Shared;

public class VatPercentageTests
{
    [Fact]
    public void Constructor_ShouldSucceed_WhenValueIsMidRange()
    {
        // Act
        var vat = new VatPercentage(10m);

        // Assert
        Assert.Equal(10m, vat.Value);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenValueIsLowerBoundary()
    {
        // Act
        var vat = new VatPercentage(0m);

        // Assert
        Assert.Equal(0m, vat.Value);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenValueIsUpperBoundary()
    {
        // Act
        var vat = new VatPercentage(100m);

        // Assert
        Assert.Equal(100m, vat.Value);
    }

    [Fact]
    public void Constructor_ShouldThrowValidationException_WhenValueIsNegative()
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new VatPercentage(-1m));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("value", error.Key);
        Assert.Equal("VAT percentage must be between 0 and 100.", Assert.Single(error.Value));
    }

    [Fact]
    public void Constructor_ShouldThrowValidationException_WhenValueIsAboveMaximum()
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new VatPercentage(101m));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("value", error.Key);
        Assert.Equal("VAT percentage must be between 0 and 100.", Assert.Single(error.Value));
    }

    [Fact]
    public void Update_ShouldMutateValue()
    {
        // Arrange
        var vat = new VatPercentage(10m);

        // Act
        vat.Update(20m);

        // Assert
        Assert.Equal(20m, vat.Value);
    }

    [Fact]
    public void Update_ShouldThrowValidationException_WhenValueIsOutOfRange()
    {
        // Arrange
        var vat = new VatPercentage(10m);

        // Act
        var ex = Assert.Throws<ValidationException>(() => vat.Update(-1m));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("value", error.Key);
        Assert.Equal("VAT percentage must be between 0 and 100.", Assert.Single(error.Value));
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenValueIsTheSame()
    {
        // Arrange
        var a = new VatPercentage(10m);
        var b = new VatPercentage(10m);

        // Assert
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenValueDiffers()
    {
        // Arrange
        var a = new VatPercentage(10m);
        var b = new VatPercentage(20m);

        // Assert
        Assert.NotEqual(a, b);
    }
}
