using StarterKit.Modules.Identity.Services;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Services;

public class ClaimComparerTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenTypeAndValueMatch()
    {
        // Arrange
        var a = new Claim("permission", "orders.view");
        var b = new Claim("permission", "orders.view");

        // Act & Assert
        Assert.True(ClaimComparer.Instance.Equals(a, b));
        Assert.Equal(ClaimComparer.Instance.GetHashCode(a), ClaimComparer.Instance.GetHashCode(b));
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenTypeDiffers()
    {
        // Arrange
        var a = new Claim("permission", "orders.view");
        var b = new Claim("role", "orders.view");

        // Act & Assert
        Assert.False(ClaimComparer.Instance.Equals(a, b));
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenValueDiffers()
    {
        // Arrange
        var a = new Claim("permission", "orders.view");
        var b = new Claim("permission", "orders.delete");

        // Act & Assert
        Assert.False(ClaimComparer.Instance.Equals(a, b));
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenEitherClaimIsNull()
    {
        // Arrange
        var a = new Claim("permission", "orders.view");

        // Act & Assert
        Assert.False(ClaimComparer.Instance.Equals(a, null));
        Assert.False(ClaimComparer.Instance.Equals(null, a));
        Assert.False(ClaimComparer.Instance.Equals(null, null));
    }
}
