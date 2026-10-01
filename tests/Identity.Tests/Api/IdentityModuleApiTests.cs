using Identity.Tests.TestSupport;
using Moq;
using StarterKit.Modules.Identity.Api;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Api;

public class IdentityModuleApiTests
{
    private static IdentityTestHost CreateHost() => new(useInMemoryProvider: true);

    private static IdentityModuleApi CreateApi(IdentityTestHost host) =>
        new(
            host.Context,
            host.UserManager,
            host.CreateUserService(),
            host.CreateUserQueryService());

    private static async Task<User> CreateUserAsync(IdentityTestHost host, User user)
    {
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        return user;
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnSummary_WhenUserExists()
    {
        // Arrange
        using var host = CreateHost();
        var user = await CreateUserAsync(host, new User
        {
            UserName = "jane.doe",
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Status = new ActiveStatus(ActiveStatus.State.Locked),
        });
        var api = CreateApi(host);

        // Act
        var summary = await api.GetUserAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(user.Id, summary.Id);
        Assert.Equal("jane.doe", summary.UserName);
        Assert.Equal("jane@example.com", summary.Email);
        Assert.Equal("Jane", summary.FirstName);
        Assert.Equal("Doe", summary.LastName);
        Assert.False(summary.IsActive);
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        using var host = CreateHost();
        var api = CreateApi(host);

        // Act
        var summary = await api.GetUserAsync("missing", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(summary);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnEmpty_WhenNoIdsAreGiven()
    {
        // Arrange
        using var host = CreateHost();
        await CreateUserAsync(host, new User { UserName = "jane.doe" });
        var api = CreateApi(host);

        // Act
        var summaries = await api.GetUsersAsync([], TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(summaries);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnMatchingUsers_AndSkipUnknownIds()
    {
        // Arrange
        using var host = CreateHost();
        var jane = await CreateUserAsync(host, new User { UserName = "jane.doe" });
        var john = await CreateUserAsync(host, new User { UserName = "john.doe" });
        await CreateUserAsync(host, new User { UserName = "not.requested" });
        var api = CreateApi(host);

        // Act
        var summaries = await api.GetUsersAsync(
            [jane.Id, john.Id, "missing"],
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            new[] { jane.Id, john.Id }.Order(),
            summaries.Select(s => s.Id).Order());
        Assert.All(summaries, s => Assert.True(s.IsActive));
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldMatchOnNormalizedEmail()
    {
        // Arrange
        using var host = CreateHost();
        var user = await CreateUserAsync(host, new User { UserName = "jane.doe", Email = "Jane@Example.com" });
        var api = CreateApi(host);

        // Act
        var summary = await api.FindByEmailAsync("jane@example.COM", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(user.Id, summary.Id);
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnNull_WhenEmailIsUnknown()
    {
        // Arrange
        using var host = CreateHost();
        await CreateUserAsync(host, new User { UserName = "jane.doe", Email = "jane@example.com" });
        var api = CreateApi(host);

        // Act
        var summary = await api.FindByEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(summary);
    }

    [Fact]
    public async Task GetUserIdsWithPermissionAsync_ShouldReturnUsersWithDirectOrRoleClaim()
    {
        // Arrange
        using var host = CreateHost();
        const string permission = "orders.view";

        var role = new Role { Name = "Manager" };
        Assert.True((await host.RoleManager.CreateAsync(role)).Succeeded);
        Assert.True((await host.RoleManager.AddClaimAsync(
            role,
            new Claim(ClaimTypeConstants.Permission, permission))).Succeeded);

        var direct = await CreateUserAsync(host, new User { UserName = "direct" });
        Assert.True((await host.UserManager.AddClaimAsync(
            direct,
            new Claim(ClaimTypeConstants.Permission, permission))).Succeeded);

        var viaRole = await CreateUserAsync(host, new User { UserName = "via.role" });
        Assert.True((await host.UserManager.AddToRoleAsync(viaRole, "Manager")).Succeeded);

        var other = await CreateUserAsync(host, new User { UserName = "other" });
        Assert.True((await host.UserManager.AddClaimAsync(
            other,
            new Claim(ClaimTypeConstants.Permission, "orders.delete"))).Succeeded);

        var api = CreateApi(host);

        // Act
        var userIds = await api.GetUserIdsWithPermissionAsync(permission, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            new[] { direct.Id, viaRole.Id }.Order(),
            userIds.Order());
    }

    [Fact]
    public async Task EnsureUserAsync_ShouldReturnExistingUserId_WithoutProvisioning()
    {
        // Arrange
        using var host = CreateHost();
        var existing = await CreateUserAsync(host, new User { UserName = "jane.doe", Email = "jane@example.com" });
        host.EventBus.Invocations.Clear();
        var api = CreateApi(host);

        // Act
        var result = await api.EnsureUserAsync("jane@example.com", "Jane", "Doe", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Data);
        Assert.Single(host.UserManager.Users);
        Assert.False(host.IntegrationEvents.HasEvents);
        host.EventBus.Verify(
            b => b.Publish(It.IsAny<UserProvisionedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureUserAsync_ShouldProvisionNewUser_AndPublishEvent_WhenEmailIsUnknown()
    {
        // Arrange
        using var host = CreateHost();
        var api = CreateApi(host);

        // Act
        var result = await api.EnsureUserAsync("jane@example.com", "Jane", "Doe", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var created = await host.UserManager.FindByIdAsync(result.Data);
        Assert.NotNull(created);
        Assert.Equal("jane@example.com", created.UserName);
        Assert.Equal("jane@example.com", created.Email);
        Assert.Equal("Jane", created.FirstName);
        Assert.Equal("Doe", created.LastName);
        Assert.False(await host.UserManager.HasPasswordAsync(created));
        host.EventBus.Verify(
            b => b.Publish(
                It.Is<UserProvisionedIntegrationEvent>(e =>
                    e.UserId == created.Id
                    && e.Email == "jane@example.com"
                    && e.Source == ProvisioningSource.Admin),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureUserAsync_ShouldThrow_WhenEmailIsBlank(string email)
    {
        // Arrange
        using var host = CreateHost();
        var api = CreateApi(host);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => api.EnsureUserAsync(email, null, null, TestContext.Current.CancellationToken));
    }
}
