using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Framework.Tests.Shared.Authorization;

public class AccessControlTests
{
    private sealed class FakeCurrentUser(string? username) : ICurrentUser
    {
        public string? SessionId => "s-1";
        public string? UserId => "u-1";
        public string? Username => username;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }

    [Fact]
    public void IsFullControl_ICurrentUser_ShouldBeTrue_OnlyForSuperUser()
    {
        // Arrange
        var superUser = new FakeCurrentUser(SuperUserPolicy.SuperUserName);
        var regularUser = new FakeCurrentUser("someone-else");

        // Act & Assert
        Assert.True(superUser.IsFullControl());
        Assert.False(regularUser.IsFullControl());
    }

    [Fact]
    public void IsFullControl_ClaimsPrincipal_ShouldReturnFalse_WhenPrincipalIsNull()
    {
        // Arrange
        ClaimsPrincipal? principal = null;

        // Act & Assert
        Assert.False(principal.IsFullControl());
    }

    [Fact]
    public void IsFullControl_ClaimsPrincipal_ShouldBeTrue_OnlyForSuperUser()
    {
        // Arrange
        var superUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypeConstants.UserName, SuperUserPolicy.SuperUserName)]));
        var regularUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypeConstants.UserName, "someone-else")]));

        // Act & Assert
        Assert.True(superUser.IsFullControl());
        Assert.False(regularUser.IsFullControl());
    }
}
