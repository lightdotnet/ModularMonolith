using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Framework.Tests.Shared.Authorization;

public class CurrentUserBaseTests
{
    private sealed class TestCurrentUser : CurrentUserBase
    { }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public void FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange
        var user = new TestCurrentUser
        {
            User = CreatePrincipal(
                new Claim(ClaimTypeConstants.FirstName, "Jane"),
                new Claim(ClaimTypeConstants.LastName, "Doe"))
        };

        // Act & Assert
        Assert.Equal("Jane Doe", user.FullName);
    }

    [Fact]
    public void HasPermission_ShouldReturnTrue_WhenExplicitPermissionClaimMatches()
    {
        // Arrange
        var user = new TestCurrentUser
        {
            User = CreatePrincipal(new Claim(ClaimTypeConstants.Permission, "orders.view"))
        };

        // Act & Assert
        Assert.True(user.HasPermission("orders.view"));
        Assert.False(user.HasPermission("orders.delete"));
    }

    [Fact]
    public void HasPermission_ShouldReturnTrue_ForSuperUser_EvenWithoutExplicitClaim()
    {
        // Arrange
        var user = new TestCurrentUser
        {
            User = CreatePrincipal(new Claim(ClaimTypeConstants.UserName, SuperUserPolicy.SuperUserName))
        };

        // Act & Assert
        Assert.True(user.HasPermission("anything.at.all"));
    }

    [Fact]
    public void IsAuthenticatedAndIsInRole_ShouldReturnFalse_WhenUserIsNull()
    {
        // Arrange
        var user = new TestCurrentUser();

        // Act & Assert
        Assert.False(user.IsAuthenticated);
        Assert.False(user.IsInRole("admin"));
    }
}
