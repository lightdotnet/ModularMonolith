using System.Reflection;
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
    public void Constructor_ShouldThrowValidationException_WhenCurrencyIsNotDefault()
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new Money(100m, "USD"));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("currency", error.Key);
        Assert.Equal($"Currency must be '{CurrencyConstants.Default}'.", Assert.Single(error.Value));
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
    public void Update_ShouldThrowValidationException_WhenCurrencyIsNotDefault()
    {
        // Arrange
        var money = new Money(100m, CurrencyConstants.Default);

        // Act
        var ex = Assert.Throws<ValidationException>(() => money.Update(100m, "USD"));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("currency", error.Key);
        Assert.Equal($"Currency must be '{CurrencyConstants.Default}'.", Assert.Single(error.Value));
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
        // The public/internal API only ever accepts CurrencyConstants.Default, so a
        // non-default currency can't be reached through Money(...) or Update(...) without
        // throwing. Materialise a second instance the same way EF does (private parameterless
        // ctor + property assignment) purely to exercise GetEqualityComponents' currency
        // component in isolation.
        var a = new Money(100m, CurrencyConstants.Default);
        var b = (Money)Activator.CreateInstance(typeof(Money), nonPublic: true)!;
        SetPrivateProperty(b, nameof(Money.Amount), 100m);
        SetPrivateProperty(b, nameof(Money.Currency), "USD");

        // Assert
        Assert.NotEqual(a, b);
    }

    private static void SetPrivateProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} not found on {target.GetType().Name}.");

        property.SetValue(target, value);
    }
}
