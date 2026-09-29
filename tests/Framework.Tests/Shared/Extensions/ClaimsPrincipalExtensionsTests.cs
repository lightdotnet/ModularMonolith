using StarterKit.Shared.Constants;
using StarterKit.Shared.Extensions;
using System.Security.Claims;
using Xunit;

namespace Framework.Tests.Shared.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public void GetUserId_ShouldReturnNull_WhenPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act
        var result = principal!.GetUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserId_ShouldReturnNull_WhenClaimIsMissing()
    {
        // Arrange
        var principal = CreatePrincipal();

        // Act & Assert
        Assert.Null(principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ShouldReturnValue_WhenClaimIsPresent()
    {
        // Arrange
        var principal = CreatePrincipal(new Claim(ClaimTypeConstants.UserId, "user-1"));

        // Act & Assert
        Assert.Equal("user-1", principal.GetUserId());
    }

    [Fact]
    public void GetEmail_ShouldReturnNull_WhenPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act & Assert
        Assert.Null(principal!.GetEmail());
    }

    [Fact]
    public void GetExpiration_ShouldThrowArgumentNullException_WhenPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => principal!.GetExpiration());
    }

    [Fact]
    public void GetExpiration_ShouldReturnUnixEpoch_WhenClaimIsMissing()
    {
        // Arrange
        var principal = CreatePrincipal();

        // Act
        var expiration = principal.GetExpiration();

        // Assert
        Assert.Equal(DateTimeOffset.UnixEpoch, expiration);
    }

    [Fact]
    public void GetExpiration_ShouldReturnDecodedValue_WhenClaimIsPresent()
    {
        // Arrange
        var expected = DateTimeOffset.UtcNow.AddHours(1);
        var principal = CreatePrincipal(new Claim(ClaimTypeConstants.Expiration, expected.ToUnixTimeSeconds().ToString()));

        // Act
        var expiration = principal.GetExpiration();

        // Assert
        Assert.Equal(expected.ToUnixTimeSeconds(), expiration.ToUnixTimeSeconds());
    }

    [Fact]
    public void IsAuthenticated_ShouldThrowNullReferenceException_WhenPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => principal!.IsAuthenticated());
    }

    [Fact]
    public void HasPermission_ShouldThrowNullReferenceException_WhenPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => principal!.HasPermission("some.permission"));
    }

    [Fact]
    public void HasPermission_ShouldReturnTrue_WhenPermissionClaimMatches()
    {
        // Arrange
        var principal = CreatePrincipal(new Claim(ClaimTypeConstants.Permission, "orders.view"));

        // Act & Assert
        Assert.True(principal.HasPermission("orders.view"));
        Assert.False(principal.HasPermission("orders.delete"));
    }
}
