using Identity.Tests.TestSupport;
using Moq;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Features.Users.Commands;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared.Constants;
using System.Security.Claims;
using Xunit;

namespace Identity.Tests.Features.Users.Commands;

public class ForcePasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnGuardError_AndNotCallService_WhenTheTargetIsAboveTheActor()
    {
        // Arrange
        using var host = new IdentityTestHost(new FakeCurrentUser { UserId = "actor-id" });
        var user = new User { UserName = "boss" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        Assert.True((await host.UserManager.AddClaimAsync(
            user,
            new Claim(ClaimTypeConstants.Permission, "roles.manage"))).Succeeded);
        var userServiceMock = new Mock<IUserService>();
        var handler = new ForcePasswordCommandHandler(userServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new ForcePasswordCommand(user.Id, "new-password"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(
            s => s.ForcePasswordAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
