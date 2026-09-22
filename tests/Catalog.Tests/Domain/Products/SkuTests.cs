using Light.Exceptions;
using StarterKit.Catalog.Api.Domain.Products;
using Xunit;

namespace Catalog.Tests.Domain.Products;

public class SkuTests
{
    [Fact]
    public void Constructor_ShouldSucceed_AndTrimWhitespace()
    {
        // Act
        var sku = new Sku("  SKU-001  ");

        // Assert
        Assert.Equal("SKU-001", sku.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowValidationException_WhenValueIsBlank(string value)
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new Sku(value));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("value", error.Key);
        Assert.Equal("SKU cannot be blank.", Assert.Single(error.Value));
    }

    [Fact]
    public void Constructor_ShouldThrowValidationException_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', Sku.MaxLength + 1);

        // Act
        var ex = Assert.Throws<ValidationException>(() => new Sku(value));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("value", error.Key);
        Assert.Equal($"SKU cannot exceed {Sku.MaxLength} characters.", Assert.Single(error.Value));
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenValueIsExactlyMaxLength()
    {
        // Arrange
        var value = new string('a', Sku.MaxLength);

        // Act
        var sku = new Sku(value);

        // Assert
        Assert.Equal(value, sku.Value);
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenValueIsTheSame()
    {
        // Arrange
        var a = new Sku("SKU-001");
        var b = new Sku("SKU-001");

        // Assert
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenValueDiffers()
    {
        // Arrange
        var a = new Sku("SKU-001");
        var b = new Sku("SKU-002");

        // Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var sku = new Sku("SKU-001");

        // Act & Assert
        Assert.Equal("SKU-001", sku.ToString());
    }
}
