using Light.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Authorization.Internal;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Framework.Tests.Shared.Authorization;

public class AuthorizationHandlerTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, string permission) =>
        new([new PermissionRequirement(permission)], user, resource: null);

    [Fact]
    public async Task HandleAsync_ShouldSucceed_ForSuperUser_WithoutExplicitPermissionClaim()
    {
        // Arrange
        var user = CreatePrincipal(new Claim(ClaimTypeConstants.UserName, SuperUserPolicy.SuperUserName));
        var context = CreateContext(user, "orders.view");
        var handler = new AuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasExactPermissionClaim()
    {
        // Arrange
        var user = CreatePrincipal(new Claim(ClaimTypeConstants.Permission, "orders.view"));
        var context = CreateContext(user, "orders.view");
        var handler = new AuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSucceed_WhenUserHasNeitherPermissionNorFullControl()
    {
        // Arrange
        var user = CreatePrincipal();
        var context = CreateContext(user, "orders.view");
        var handler = new AuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
