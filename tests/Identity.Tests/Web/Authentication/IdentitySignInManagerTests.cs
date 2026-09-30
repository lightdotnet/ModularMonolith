using Identity.Tests.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Web.Authentication;
using StarterKit.Shared;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Web.Authentication;

public class IdentitySignInManagerTests
{
    private static IdentitySignInManager CreateSignInManager(IdentityTestHost host)
    {
        var options = Options.Create(host.UserManager.Options);

        return new IdentitySignInManager(
            host.UserManager,
            new HttpContextAccessor(),
            new UserClaimsPrincipalFactory<User>(host.UserManager, options),
            options,
            NullLogger<SignInManager<User>>.Instance,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new DefaultUserConfirmation<User>());
    }

    private static async Task<User> CreateUserAsync(IdentityTestHost host)
    {
        var user = new User { UserName = "jane.doe" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        return user;
    }

    private static ClaimsPrincipal CreatePrincipal(
        IdentityTestHost host,
        string userId,
        string securityStamp)
    {
        var claimsIdentity = host.UserManager.Options.ClaimsIdentity;

        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(claimsIdentity.UserIdClaimType, userId),
                new Claim(claimsIdentity.SecurityStampClaimType, securityStamp),
            ],
            IdentityConstants.ApplicationScheme));
    }

    [Fact]
    public async Task CanSignInAsync_ShouldReturnTrue_WhenUserIsActive()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = await CreateUserAsync(host);
        var signInManager = CreateSignInManager(host);

        // Act
        var canSignIn = await signInManager.CanSignInAsync(user);

        // Assert
        Assert.True(canSignIn);
    }

    [Fact]
    public async Task CanSignInAsync_ShouldReturnFalse_WhenUserIsLocked()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = await CreateUserAsync(host);
        user.UpdateStatus(ActiveStatus.State.Locked);
        var signInManager = CreateSignInManager(host);

        // Act
        var canSignIn = await signInManager.CanSignInAsync(user);

        // Assert
        Assert.False(canSignIn);
    }

    [Fact]
    public async Task CanSignInAsync_ShouldReturnFalse_WhenUserIsDeleted()
    {
        // Arrange: only the in-memory entity is marked deleted - the check reads the entity as given.
        using var host = new IdentityTestHost();
        var user = await CreateUserAsync(host);
        user.Deleted = DateTimeOffset.UtcNow;
        var signInManager = CreateSignInManager(host);

        // Act
        var canSignIn = await signInManager.CanSignInAsync(user);

        // Assert
        Assert.False(canSignIn);
    }

    [Fact]
    public async Task ValidateSecurityStampAsync_ShouldReturnUser_WhenStampIsValidAndUserIsActive()
    {
        // Arrange
        using var host = new IdentityTestHost();
        var user = await CreateUserAsync(host);
        var stamp = await host.UserManager.GetSecurityStampAsync(user);
        var signInManager = CreateSignInManager(host);

        // Act
        var validated = await signInManager.ValidateSecurityStampAsync(CreatePrincipal(host, user.Id, stamp));

        // Assert
        Assert.NotNull(validated);
        Assert.Equal(user.Id, validated!.Id);
    }

    [Fact]
    public async Task ValidateSecurityStampAsync_ShouldReturnNull_WhenUserIsLockedEvenWithValidStamp()
    {
        // Arrange: lock the user without rotating the stamp, so only the status check can reject.
        using var host = new IdentityTestHost();
        var user = await CreateUserAsync(host);
        user.UpdateStatus(ActiveStatus.State.Locked);
        Assert.True((await host.UserManager.UpdateAsync(user)).Succeeded);
        var stamp = await host.UserManager.GetSecurityStampAsync(user);
        var signInManager = CreateSignInManager(host);

        // Act
        var validated = await signInManager.ValidateSecurityStampAsync(CreatePrincipal(host, user.Id, stamp));

        // Assert
        Assert.Null(validated);
    }
}
