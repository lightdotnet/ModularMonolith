using Light.Exceptions;
using StarterKit.Catalog.Api.Domain.Products;
using Xunit;

namespace Catalog.Tests.Domain.Products;

public class ProductImageUrlTests
{
    [Fact]
    public void Constructor_ShouldSucceed_WithSortOrder()
    {
        // Act
        var image = new ProductImageUrl("https://example.com/a.png", 2);

        // Assert
        Assert.Equal("https://example.com/a.png", image.Url);
        Assert.Equal(2, image.SortOrder);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WithoutSortOrder()
    {
        // Act
        var image = new ProductImageUrl("https://example.com/a.png");

        // Assert
        Assert.Equal("https://example.com/a.png", image.Url);
        Assert.Null(image.SortOrder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowValidationException_WhenUrlIsBlank(string url)
    {
        // Act
        var ex = Assert.Throws<ValidationException>(() => new ProductImageUrl(url));

        // Assert
        var error = Assert.Single(ex.ValidationErrors);
        Assert.Equal("url", error.Key);
        Assert.Equal("Image URL cannot be blank.", Assert.Single(error.Value));
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenUrlAndSortOrderAreTheSame()
    {
        // Arrange
        var a = new ProductImageUrl("https://example.com/a.png", 1);
        var b = new ProductImageUrl("https://example.com/a.png", 1);

        // Assert
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ShouldBeFalse_WhenUrlDiffers()
    {
        // Arrange
        var a = new ProductImageUrl("https://example.com/a.png", 1);
        var b = new ProductImageUrl("https://example.com/b.png", 1);

        // Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_ShouldBeTrue_WhenSortOrderIsNullAndMinusOne()
    {
        // Arrange: GetEqualityComponents yields `SortOrder ?? -1`, so a null sort order and an
        // explicit -1 are indistinguishable by this equality implementation — a real, slightly
        // surprising consequence of the `?? -1` sentinel, asserted here deliberately.
        var withNullSortOrder = new ProductImageUrl("https://example.com/a.png");
        var withMinusOneSortOrder = new ProductImageUrl("https://example.com/a.png", -1);

        // Assert
        Assert.Equal(withNullSortOrder, withMinusOneSortOrder);
    }
}
