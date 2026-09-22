using Light.Exceptions;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Framework.Tests.Shared;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldSucceed_WhenAmountIsNonNegativeAndCurrencyIsDefault()
    {
        // Act
        var money = new Money(100m, CurrencyConstants.Default);

        // Assert
        Assert.Equal(100m, money.Amount);
        Assert.Equal(CurrencyConstants.Default, money.Currency);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenAmountIsZero()
    {
        // Act
        var money = new Money(0m, CurrencyConstants.Default);

        // Assert
        Assert.Equal(0m, money.Amount);
        Assert.Equal(CurrencyConstants.Default, money.Currency);
    }

    [Fact]
    public void Constructor_ShouldThrowValidationException_WhenAmountIsNegative()
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new Money(-1m, CurrencyConstants.Default));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("amount", error.Key);
        Assert.Equal("Amount cannot be negative.", Assert.Single(error.Value));
    }

    [Fact]
    public void Constructor_ShouldAcceptAnyWellFormedCurrencyCode()
    {
        // Act
        var money = new Money(100m, "USD");

        // Assert
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_ShouldTrimAndUpperCaseTheCurrency()
    {
        // Act
        var money = new Money(100m, " usd ");

        // Assert
        Assert.Equal("USD", money.Currency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("U1D")]
    [InlineData("€€€")]
    public void Constructor_ShouldThrowValidationException_WhenCurrencyIsMalformed(string currency)
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new Money(100m, currency));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("currency", error.Key);
        Assert.Equal("Currency must be a three-letter ISO 4217 code.", Assert.Single(error.Value));
    }

    [Fact]
    public void Update_ShouldMutateAmountAndCurrency()
    {
        // Arrange
        var money = new Money(100m, CurrencyConstants.Default);

        // Act
        money.Update(200m, CurrencyConstants.Default);

        // Assert
        Assert.Equal(200m, money.Amount);
        Assert.Equal(CurrencyConstants.Default, money.Currency);
    }

    [Fact]
    public void Update_ShouldThrowValidationException_WhenAmountIsNegative()
    {
        // Arrange
        var money = new Money(100m, CurrencyConstants.Default);

        // Act
        var ex = Assert.Throws<ValidationException>(() => money.Update(-1m, CurrencyConstants.Default));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("amount", error.Key);
        Assert.Equal("Amount cannot be negative.", Assert.Single(error.Value));
    }

    [Fact]
    public void Update_ShouldAcceptAnyWellFormedCurrencyCode()
    {
        // Arrange
        var money = new Money(100m, CurrencyConstants.Default);

        // Act
        money.Update(100m, "usd");

        // Assert
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Update_ShouldThrowValidationException_WhenCurrencyIsMalformed()
    {
        // Arrange
        var money = new Money(100m, CurrencyConstants.Default);

        // Act
        var ex = Assert.Throws<ValidationException>(() => money.Update(100m, "US"));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("currency", error.Key);
        Assert.Equal("Currency must be a three-letter ISO 4217 code.", Assert.Single(error.Value));
        Assert.Equal(CurrencyConstants.Default, money.Currency);
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenAmountAndCurrencyAreTheSame()
    {
        // Arrange
        var a = new Money(100m, CurrencyConstants.Default);
        var b = new Money(100m, CurrencyConstants.Default);

        // Assert
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenAmountDiffers()
    {
        // Arrange
        var a = new Money(100m, CurrencyConstants.Default);
        var b = new Money(200m, CurrencyConstants.Default);

        // Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenCurrencyDiffers()
    {
        // Arrange
        var a = new Money(100m, CurrencyConstants.Default);
        var b = new Money(100m, "USD");

        // Assert
        Assert.NotEqual(a, b);
    }
}
