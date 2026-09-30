using Identity.Tests.TestSupport;
using StarterKit.Modules.Identity.Authentication.Jwt;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared.Constants;
using Xunit;

namespace Identity.Tests.Authentication.Jwt;

public class JwtTokenIssuerTests
{
    [Fact]
    public async Task IssueAsync_ShouldIncludeUserAndTokenIdClaims_PlusRoleDerivedPermissions()
    {
        // Arrange
        using var host = new IdentityTestHost();

        var role = new Role { Name = "Manager" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        Assert.True((await host.RoleManager.AddClaimAsync(role, new System.Security.Claims.Claim(ClaimTypeConstants.Permission, "orders.view"))).Succeeded);

        var user = new User { UserName = "manager.jane" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        Assert.True((await host.UserManager.AddToRoleAsync(user, "Manager")).Succeeded);

        var signingService = new JwtSigningService(TestJwtOptions.Create());
        var issuer = new JwtTokenIssuer(host.UserManager, host.Context, signingService);

        // Act
        var token = await issuer.IssueAsync(user, "session-123", DateTime.UtcNow.AddHours(1));
        var claims = JwtHelper.ReadClaims(token);

        // Assert
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.UserId && c.Value == user.Id);
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.UserName && c.Value == "manager.jane");
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.TokenId && c.Value == "session-123");
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.Permission && c.Value == "orders.view");
    }

    [Fact]
    public async Task IssueAsync_ShouldNotIncludePermissionClaims_WhenUserHasNoRoles()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "no.roles" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);

        var signingService = new JwtSigningService(TestJwtOptions.Create());
        var issuer = new JwtTokenIssuer(host.UserManager, host.Context, signingService);

        // Act
        var token = await issuer.IssueAsync(user, "session-456", DateTime.UtcNow.AddHours(1));
        var claims = JwtHelper.ReadClaims(token);

        // Assert
        Assert.DoesNotContain(claims, c => c.Type == ClaimTypeConstants.Permission);
        Assert.Contains(claims, c => c.Type == ClaimTypeConstants.UserId && c.Value == user.Id);
    }
}
