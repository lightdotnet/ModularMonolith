using Identity.Tests.TestSupport;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.Services;
using StarterKit.Identity.Contracts;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Services;

public class RoleServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateRole()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.CreateAsync(
            new CreateRoleRequest { Name = "Admin", Description = "Administrators" });

        // Assert
        Assert.True(result.IsSuccess);
        var created = await host.RoleManager.FindByIdAsync(result.Data);
        Assert.NotNull(created);
        Assert.Equal("Admin", created!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.GetByIdAsync("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnRoleWithClaims_WhenFound()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var role = new Role { Name = "Admin", Description = "Administrators" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        Assert.True((await host.RoleManager.AddClaimAsync(role, new Claim("perm", "orders.view"))).Succeeded);
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.GetByNameAsync("Admin");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Administrators", result.Data!.Description);
        Assert.Contains(result.Data!.Claims, c => c.Type == "perm" && c.Value == "orders.view");
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.GetByNameAsync("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateNameAndDescription_AndSyncClaims()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var role = new Role { Name = "Admin", Description = "Old description" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        Assert.True((await host.RoleManager.AddClaimAsync(role, new Claim("old", "value"))).Succeeded);
        var service = new RoleService(host.RoleManager);

        var request = new RoleDto
        {
            Id = role.Id,
            Name = "Super Admin",
            Description = "New description",
            Claims = [new ClaimDto { Type = "new", Value = "value" }],
        };

        // Act
        var result = await service.UpdateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.RoleManager.FindByIdAsync(role.Id);
        Assert.Equal("Super Admin", reloaded!.Name);
        Assert.Equal("New description", reloaded.Description);
        var claims = await host.RoleManager.GetClaimsAsync(reloaded);
        Assert.DoesNotContain(claims, c => c.Type == "old");
        Assert.Contains(claims, c => c.Type == "new" && c.Value == "value");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.UpdateAsync(new RoleDto { Id = "missing", Name = "x" });

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenRoleStillHasClaims()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var role = new Role { Name = "Admin" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        Assert.True((await host.RoleManager.AddClaimAsync(role, new Claim("perm", "orders.view"))).Succeeded);
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.DeleteAsync(role.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(await host.RoleManager.FindByIdAsync(role.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenRoleHasNoClaims()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var role = new Role { Name = "Empty" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.DeleteAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(await host.RoleManager.FindByIdAsync(role.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var service = new RoleService(host.RoleManager);

        // Act
        var result = await service.DeleteAsync("missing");

        // Assert
        Assert.False(result.IsSuccess);
    }
}
