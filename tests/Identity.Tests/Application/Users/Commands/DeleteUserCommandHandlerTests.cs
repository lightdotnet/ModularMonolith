using Identity.Tests.TestSupport;
using Light.Contracts;
using Moq;
using StarterKit.Modules.Identity.Application.Users.Commands;
using StarterKit.Modules.Identity.Application.Users.Services;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared.Authorization;
using Xunit;

namespace Identity.Tests.Application.Users.Commands;

public class DeleteUserCommandHandlerTests
{
    private static FakeCurrentUser SuperActor() => new()
    {
        UserId = "actor-id",
        Username = SuperUserPolicy.SuperUserName,
        IsAuthenticated = true,
    };

    [Fact]
    public async Task Handle_ShouldCallService_WhenTheGuardAllows()
    {
        // Arrange
        using var host = new IdentityTestHost(SuperActor());
        var user = new User { UserName = "jane" };
        Assert.True((await host.UserManager.CreateAsync(user)).Succeeded);
        var userServiceMock = new Mock<IUserService>();
        userServiceMock.Setup(s => s.DeleteAsync(user.Id)).ReturnsAsync(Result.Success());
        var handler = new DeleteUserCommandHandler(userServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        userServiceMock.Verify(s => s.DeleteAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_AndNotCallService_WhenDeletingOwnAccount()
    {
        // Arrange
        using var host = new IdentityTestHost(SuperActor());
        var userServiceMock = new Mock<IUserService>();
        var handler = new DeleteUserCommandHandler(userServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new DeleteUserCommand("actor-id"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_AndNotCallService_WhenDeletingASuperUser()
    {
        // Arrange
        using var host = new IdentityTestHost(SuperActor());
        var super = new User { UserName = SuperUserPolicy.SuperUserName };
        Assert.True((await host.UserManager.CreateAsync(super)).Succeeded);
        var userServiceMock = new Mock<IUserService>();
        var handler = new DeleteUserCommandHandler(userServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new DeleteUserCommand(super.Id), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnServiceNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange: the guard passes a missing target through to the service.
        using var host = new IdentityTestHost(new FakeCurrentUser { UserId = "actor-id" });
        var userServiceMock = new Mock<IUserService>();
        var expected = Result.NotFound("User missing not found");
        userServiceMock.Setup(s => s.DeleteAsync("missing")).ReturnsAsync(expected);
        var handler = new DeleteUserCommandHandler(userServiceMock.Object, host.CreatePermissionGrantGuard());

        // Act
        var result = await handler.Handle(new DeleteUserCommand("missing"), CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
    }
}
