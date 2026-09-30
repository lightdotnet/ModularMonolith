using Identity.Tests.TestSupport;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Models;
using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Authorization;

public class PermissionGrantGuardTests
{
    private const string UsersView = "users.view";
    private const string UsersUpdate = "users.update";
    private const string RolesManage = "roles.manage";

    private static FakeCurrentUser Actor(params string[] permissions)
    {
        var actor = new FakeCurrentUser
        {
            UserId = "actor-id",
            Username = "admin",
            IsAuthenticated = true,
        };

        actor.Permissions.UnionWith(permissions);
        return actor;
    }

    private static Claim Permission(string value) => new(ClaimTypeConstants.Permission, value);

    private static ClaimDto PermissionDto(string value) => new()
    {
        Type = ClaimTypeConstants.Permission,
        Value = value,
    };

    private static async Task<Role> CreateRoleAsync(
        IdentityTestHost host,
        string name,
        params string[] permissions)
    {
        var role = new Role { Name = name };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);

        foreach (var permission in permissions)
            Assert.True((await host.RoleManager.AddClaimAsync(role, Permission(permission))).Succeeded);

        return role;
    }

    private static async Task<User> CreateUserAsync(
        IdentityTestHost host,
        string userName,
        string? id = null,
        string[]? roles = null,
        string[]? permissions = null)
    {
        var user = new User { UserName = userName };
        if (id is not null)
            user.Id = id;

        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);

        foreach (var role in roles ?? [])
            Assert.True((await host.UserManager.AddToRoleAsync(user, role)).Succeeded);

        foreach (var permission in permissions ?? [])
            Assert.True((await host.UserManager.AddClaimAsync(user, Permission(permission))).Succeeded);

        return user;
    }

    private static UserDto ToDto(
        User user,
        IEnumerable<string>? roles = null,
        IEnumerable<ClaimDto>? claims = null,
        string? status = null) => new()
        {
            Id = user.Id,
            UserName = user.UserName!,
            Status = status ?? user.Status.Value.ToString(),
            Roles = [.. roles ?? []],
            Claims = [.. claims ?? []],
        };

    [Fact]
    public async Task CanUpdateUserAsync_ShouldAllow_WhenGrantingAHeldPermissionDirectly()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var user = await CreateUserAsync(host, "jane");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(user, claims: [PermissionDto(UsersView)]));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenGrantingAnUnheldPermissionDirectly()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var user = await CreateUserAsync(host, "jane");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(user, claims: [PermissionDto(RolesManage)]));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(RolesManage, result.Message);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldAllow_WhenAssigningARoleWithHeldPermissions()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView, UsersUpdate));
        await CreateRoleAsync(host, "Editors", UsersView, UsersUpdate);
        var user = await CreateUserAsync(host, "jane");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(user, roles: ["Editors"]));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenAssigningARoleWithAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        await CreateRoleAsync(host, "Admins", UsersView, RolesManage);
        var user = await CreateUserAsync(host, "jane");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(user, roles: ["Admins"]));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(RolesManage, result.Message);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenRemovingARoleWithAnUnheldPermission()
    {
        // Arrange: a permission the target holds and the actor lacks also makes the target
        // "above" the actor, so either rule rejects the revocation.
        using var host = new IdentityTestHost(Actor(UsersView));
        await CreateRoleAsync(host, "Admins", RolesManage);
        var user = await CreateUserAsync(host, "jane", roles: ["Admins"]);
        var guard = host.CreatePermissionGrantGuard();

        // Act: removing the role revokes roles.manage, which the actor does not hold.
        var result = await guard.CanUpdateUserAsync(ToDto(user, roles: []));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(RolesManage, result.Message);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenTheTargetHoldsPermissionsTheActorLacks()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var user = await CreateUserAsync(host, "boss", permissions: [UsersView, RolesManage]);
        var guard = host.CreatePermissionGrantGuard();

        // Act: a profile-only edit (claims unchanged) is still managing a higher-privileged user.
        var result = await guard.CanUpdateUserAsync(ToDto(
            user,
            claims: [PermissionDto(UsersView), PermissionDto(RolesManage)]));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(RolesManage, result.Message);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenTheTargetIsASuperUser()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var user = await CreateUserAsync(host, SuperUserPolicy.SuperUserName);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(user));

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenChangingOwnRoles()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        await CreateRoleAsync(host, "Viewers", UsersView);
        var self = await CreateUserAsync(host, "admin", id: "actor-id");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(self, roles: ["Viewers"]));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("own roles", result.Message);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBlock_WhenChangingOwnStatus()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var self = await CreateUserAsync(host, "admin", id: "actor-id");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(
            self,
            status: nameof(ActiveStatus.State.Locked)));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("own status", result.Message);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldAllow_WhenEditingOwnProfileOnly()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        await CreateRoleAsync(host, "Viewers", UsersView);
        var self = await CreateUserAsync(host, "admin", id: "actor-id", roles: ["Viewers"]);
        var guard = host.CreatePermissionGrantGuard();
        var model = ToDto(self, roles: ["Viewers"]);
        model.FirstName = "New";
        model.Email = "new@example.com";

        // Act
        var result = await guard.CanUpdateUserAsync(model);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldBypassChecks_WhenTheActorHasFullControl()
    {
        // Arrange
        var actor = Actor();
        actor.Username = SuperUserPolicy.SuperUserName;
        using var host = new IdentityTestHost(actor);
        await CreateRoleAsync(host, "Admins", RolesManage);
        var user = await CreateUserAsync(host, "jane", permissions: [UsersUpdate]);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(ToDto(
            user,
            roles: ["Admins"],
            claims: [PermissionDto(RolesManage)]));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateUserAsync_ShouldPassThrough_WhenTheUserDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor());
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateUserAsync(new UserDto { Id = "missing", UserName = "missing" });

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanForcePasswordAsync_ShouldBlock_WhenTheTargetHoldsPermissionsTheActorLacks()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        await CreateRoleAsync(host, "Admins", RolesManage);
        var user = await CreateUserAsync(host, "boss", roles: ["Admins"]);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanForcePasswordAsync(user.Id);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanDeleteUserAsync_ShouldAllow_WhenTheActorHoldsEveryTargetPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var user = await CreateUserAsync(host, "jane", permissions: [UsersView]);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanDeleteUserAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanDeleteUserAsync_ShouldBlock_WhenDeletingOwnAccount()
    {
        // Arrange: even full control cannot delete its own account.
        var actor = Actor();
        actor.Username = SuperUserPolicy.SuperUserName;
        using var host = new IdentityTestHost(actor);
        await CreateUserAsync(host, SuperUserPolicy.SuperUserName, id: "actor-id");
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanDeleteUserAsync("actor-id");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanDeleteUserAsync_ShouldBlock_WhenTheTargetIsASuperUser_EvenForFullControl()
    {
        // Arrange
        var actor = Actor();
        actor.Username = SuperUserPolicy.SuperUserName;
        using var host = new IdentityTestHost(actor);
        var super = await CreateUserAsync(host, SuperUserPolicy.SuperUserName);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanDeleteUserAsync(super.Id);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateRoleAsync_ShouldAllow_WhenAddingAHeldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView, UsersUpdate));
        var role = await CreateRoleAsync(host, "Editors", UsersView);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateRoleAsync(new RoleDto
        {
            Id = role.Id,
            Name = "Editors",
            Claims = [PermissionDto(UsersView), PermissionDto(UsersUpdate)],
        });

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanUpdateRoleAsync_ShouldBlock_WhenAddingAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var role = await CreateRoleAsync(host, "Editors", UsersView);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanUpdateRoleAsync(new RoleDto
        {
            Id = role.Id,
            Name = "Editors",
            Claims = [PermissionDto(UsersView), PermissionDto(RolesManage)],
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(RolesManage, result.Message);
    }

    [Fact]
    public async Task CanUpdateRoleAsync_ShouldBlock_WhenTheRoleGrantsAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var role = await CreateRoleAsync(host, "Admins", UsersView, RolesManage);
        var guard = host.CreatePermissionGrantGuard();

        // Act: even a rename is managing a role above the actor.
        var result = await guard.CanUpdateRoleAsync(new RoleDto
        {
            Id = role.Id,
            Name = "Renamed",
            Claims = [PermissionDto(UsersView), PermissionDto(RolesManage)],
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(RolesManage, result.Message);
    }

    [Fact]
    public async Task CanDeleteRoleAsync_ShouldBlock_WhenTheRoleGrantsAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(Actor(UsersView));
        var role = await CreateRoleAsync(host, "Admins", RolesManage);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanDeleteRoleAsync(role.Id);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CanDeleteRoleAsync_ShouldBypassChecks_WhenTheActorHasFullControl()
    {
        // Arrange
        var actor = Actor();
        actor.Username = SuperUserPolicy.SuperUserName;
        using var host = new IdentityTestHost(actor);
        var role = await CreateRoleAsync(host, "Admins", RolesManage);
        var guard = host.CreatePermissionGrantGuard();

        // Act
        var result = await guard.CanDeleteRoleAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
