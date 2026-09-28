using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Authorization;
using StarterKit.WebMvc.Infrastructure;
using Xunit;

namespace StarterKit.WebMvc.Tests.Authorization;

public class PermissionCheckerTests
{
    private const string View = "identity.users.view";

    private const string Edit = "identity.users.edit";

    private const string Delete = "identity.users.delete";

    [Fact]
    public void HasPermission_ShouldRequireMatchingClaim()
    {
        // Arrange
        var checker = CreateChecker();
        var user = User(
            "alice",
            View);

        // Act + Assert
        Assert.True(checker.HasPermission(
            user,
            View));

        Assert.False(checker.HasPermission(
            user,
            Edit));
    }

    [Fact]
    public void HasAnyPermission_ShouldSucceedWhenOneMatches()
    {
        // Arrange
        var checker = CreateChecker();
        var user = User(
            "alice",
            View);

        // Act + Assert
        Assert.True(checker.HasAnyPermission(
            user,
            Edit,
            View));

        Assert.False(checker.HasAnyPermission(
            user,
            Edit,
            Delete));
    }

    [Fact]
    public void HasAllPermissions_ShouldRequireEveryPermission()
    {
        // Arrange
        var checker = CreateChecker();
        var user = User(
            "alice",
            View,
            Edit);

        // Act + Assert
        Assert.True(checker.HasAllPermissions(
            user,
            View,
            Edit));

        Assert.False(checker.HasAllPermissions(
            user,
            View,
            Delete));
    }

    [Fact]
    public void SuperAdmin_ShouldBypassEveryCheck()
    {
        // Arrange
        var checker = CreateChecker("root");
        var user = User("root");

        // Act + Assert
        Assert.True(checker.IsSuperAdmin(user));
        Assert.True(checker.HasPermission(
            user,
            Delete));

        Assert.True(checker.HasAnyPermission(
            user,
            Edit,
            Delete));

        Assert.True(checker.HasAllPermissions(
            user,
            View,
            Delete));
    }

    [Fact]
    public void SuperAdmin_ShouldMatchUserNameCaseSensitively()
    {
        // Arrange
        var checker = CreateChecker("root");

        // Act + Assert
        Assert.False(checker.IsSuperAdmin(User("ROOT")));
        Assert.False(checker.HasPermission(
            User("ROOT"),
            View));
    }

    [Fact]
    public void SuperAdmin_ShouldFollowOptionsChanges()
    {
        // Arrange
        var options = new TestOptionsMonitor(new PermissionOptions());
        var checker = new PermissionChecker(options);
        var user = User("root");

        // Act
        var before = checker.IsSuperAdmin(user);
        options.CurrentValue.SuperAdminUserNames.Add("root");
        var after = checker.IsSuperAdmin(user);

        // Assert
        Assert.False(before);
        Assert.True(after);
    }

    [Fact]
    public void UnauthenticatedUser_ShouldBeDeniedEvenWithClaims()
    {
        // Arrange
        var checker = CreateChecker("root");
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(SessionClaimTypes.UserName, "root"),
            new Claim(SessionClaimTypes.Permission, View),
        ]));

        // Act + Assert
        Assert.False(checker.HasPermission(
            anonymous,
            View));

        Assert.False(checker.HasAnyPermission(
            anonymous,
            View));

        Assert.False(checker.HasAllPermissions(
            anonymous,
            View));
    }

    [Fact]
    public async Task AuthorizationHandler_ShouldSucceedOnlyWhenPermitted()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler(CreateChecker());
        var requirement = new PermissionRequirement(View);

        var permitted = new AuthorizationHandlerContext(
            [requirement],
            User(
                "alice",
                View),
            null);

        var denied = new AuthorizationHandlerContext(
            [requirement],
            User(
                "bob",
                Edit),
            null);

        // Act
        await handler.HandleAsync(permitted);
        await handler.HandleAsync(denied);

        // Assert
        Assert.True(permitted.HasSucceeded);
        Assert.False(denied.HasSucceeded);
        Assert.False(denied.HasFailed);
    }

    [Fact]
    public async Task PolicyProvider_ShouldBuildPermissionPolicyFromPrefixedName()
    {
        // Arrange
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        // Act
        var policy = await provider.GetPolicyAsync(new HasPermissionAttribute(View).Policy!);
        var bare = await provider.GetPolicyAsync(PermissionPolicyProvider.PolicyPrefix);

        // Assert
        Assert.NotNull(policy);
        Assert.Contains(SessionDefaults.AuthenticationScheme, policy.AuthenticationSchemes);
        var requirement = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());
        Assert.Equal(View, requirement.Permission);
        Assert.Null(bare);
    }

    private static PermissionChecker CreateChecker(params string[] superAdmins)
    {
        var options = new PermissionOptions
        {
            SuperAdminUserNames = superAdmins.ToList(),
        };

        return new PermissionChecker(new TestOptionsMonitor(options));
    }

    private static ClaimsPrincipal User(
        string userName,
        params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(SessionClaimTypes.UserName, userName),
        };

        claims.AddRange(permissions.Select(permission => new Claim(
            SessionClaimTypes.Permission,
            permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            SessionDefaults.AuthenticationScheme));
    }

    private sealed class TestOptionsMonitor(
        PermissionOptions value)
        : IOptionsMonitor<PermissionOptions>
    {
        public PermissionOptions CurrentValue { get; } = value;

        public PermissionOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<PermissionOptions, string?> listener) => null;
    }
}
