using Identity.Tests.TestSupport;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.Services;
using StarterKit.Identity.Contracts;
using StarterKit.Shared;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WithPassword()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new UserService(host.UserManager);
        var request = new CreateUserRequest { UserName = "jane.doe", Email = "jane@example.com", Password = "pass123" };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var created = await host.UserManager.FindByIdAsync(result.Data);
        Assert.NotNull(created);
        Assert.True(await host.UserManager.CheckPasswordAsync(created!, "pass123"));
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WithoutPassword()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new UserService(host.UserManager);
        var request = new CreateUserRequest { UserName = "no.password", AuthProvider = "AD" };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var created = await host.UserManager.FindByIdAsync(result.Data);
        Assert.NotNull(created);
        Assert.False(await host.UserManager.HasPasswordAsync(created!));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.GetByIdAsync("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserWithRolesAndClaims_WhenFound()
    {
        // Arrange
        using var host = new IdentityTestHost();
        Assert.True((await host.RoleManager.CreateAsync(new Role { Name = "Admin" })).Succeeded);
        var user = new User { UserName = "jane.doe" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        Assert.True((await host.UserManager.AddToRoleAsync(user, "Admin")).Succeeded);
        Assert.True((await host.UserManager.AddClaimAsync(user, new Claim("dept", "eng"))).Succeeded);
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.GetByIdAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Admin", result.Data!.Roles);
        Assert.Contains(result.Data!.Claims, c => c.Type == "dept" && c.Value == "eng");
    }

    [Fact]
    public async Task GetByUserNameAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.GetByUserNameAsync("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateInfoStatusAuthProvider_AndSyncRolesAndClaims()
    {
        // Arrange
        using var host = new IdentityTestHost();
        Assert.True((await host.RoleManager.CreateAsync(new Role { Name = "Admin" })).Succeeded);
        Assert.True((await host.RoleManager.CreateAsync(new Role { Name = "Viewer" })).Succeeded);
        var user = new User { UserName = "jane.doe" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        Assert.True((await host.UserManager.AddToRoleAsync(user, "Viewer")).Succeeded);
        Assert.True((await host.UserManager.AddClaimAsync(user, new Claim("old", "value"))).Succeeded);
        var service = new UserService(host.UserManager);

        var dto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "555-0100",
            Email = "jane@example.com",
            Status = ActiveStatus.State.Locked.ToString(),
            AuthProvider = "AD",
            Roles = ["Admin"],
            Claims = [new ClaimDto { Type = "new", Value = "value" }],
        };

        // Act
        var result = await service.UpdateAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.UserManager.FindByIdAsync(user.Id);
        Assert.Equal("Jane", reloaded!.FirstName);
        Assert.Equal(ActiveStatus.State.Locked, reloaded.Status.Value);
        Assert.Equal(AuthProvider.ActiveDirectory, reloaded.AuthProvider);
        var roles = await host.UserManager.GetRolesAsync(reloaded);
        Assert.Equal(["Admin"], roles);
        var claims = await host.UserManager.GetClaimsAsync(reloaded);
        Assert.DoesNotContain(claims, c => c.Type == "old");
        Assert.Contains(claims, c => c.Type == "new" && c.Value == "value");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.UpdateAsync(new UserDto { Id = "missing", UserName = "x" });

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange: DeleteAsync scrubs PII via User.Delete() and then hard-deletes the row through
        // UserManager.DeleteAsync - it is not a soft delete despite the entity implementing ISoftDelete.
        using var host = new IdentityTestHost();
        var user = new User { UserName = "jane.doe", Email = "jane@example.com" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.DeleteAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.UserManager.FindByIdAsync(user.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.DeleteAsync("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ForcePasswordAsync_ShouldResetPassword()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = new User { UserName = "jane.doe" };
        Assert.True((await host.UserManager.CreateAsync(user, "old-pass")).Succeeded);
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.ForcePasswordAsync(user.Id, "new-pass");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(await host.UserManager.CheckPasswordAsync(user, "new-pass"));
    }

    [Fact]
    public async Task GetUsersHasClaimAsync_ShouldReturnOnlyMatchingUsers()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var withClaim = new User { UserName = "has.claim" };
        var withoutClaim = new User { UserName = "no.claim" };
        Assert.True((await host.UserManager.CreateAsync(withClaim)).Succeeded);
        Assert.True((await host.UserManager.CreateAsync(withoutClaim)).Succeeded);
        Assert.True((await host.UserManager.AddClaimAsync(withClaim, new Claim("perm", "view"))).Succeeded);
        var service = new UserService(host.UserManager);

        // Act
        var result = await service.GetUsersHasClaimAsync("perm", "view");

        // Assert
        Assert.Equal(["has.claim"], result.Select(u => u.UserName));
    }
}
