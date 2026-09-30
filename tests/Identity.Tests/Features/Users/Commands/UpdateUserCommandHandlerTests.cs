using Identity.Tests.TestSupport;
using Moq;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Features.Users.Commands;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared.Constants;
using Xunit;

namespace Identity.Tests.Features.Users.Commands;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnGuardError_AndNotCallService_WhenGrantingAnUnheldPermission()
    {
        // Arrange
        using var host = new IdentityTestHost(new FakeCurrentUser { UserId = "actor-id" });
        var user = new User { UserName = "jane" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var userServiceMock = new Mock<IUserService>();
        var handler = new UpdateUserCommandHandler(userServiceMock.Object, host.CreatePermissionGrantGuard());
        var model = new UserDto
        {
            Id = user.Id,
            UserName = "jane",
            Claims = [new ClaimDto { Type = ClaimTypeConstants.Permission, Value = "roles.manage" }],
        };

        // Act
        var result = await handler.Handle(new UpdateUserCommand(model), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(s => s.UpdateAsync(It.IsAny<UserDto>()), Times.Never);
    }
}
