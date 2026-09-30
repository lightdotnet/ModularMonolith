using Light.Contracts;
using Moq;
using StarterKit.Modules.Identity.Features.Users.Commands;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;
using Xunit;

namespace Identity.Tests.Features.Users.Commands;

/// <remarks>
/// The handler no longer publishes anything itself: <see cref="IUserService.CreateAsync"/> raises
/// the <c>UserProvisionedIntegrationEvent</c> (covered by <c>UserServiceTests</c>), so these tests
/// only pin that the handler forwards to the service and returns its result unchanged.
/// </remarks>
public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnServiceResult_WhenCreateSucceeds()
    {
        // Arrange
        var userServiceMock = new Mock<IUserService>();
        var request = new CreateUserRequest { UserName = "jane", Email = "jane@example.com" };
        var expected = Result<string>.Success("user-1");
        userServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(expected);
        var handler = new CreateUserCommandHandler(userServiceMock.Object);

        // Act
        var result = await handler.Handle(new CreateUserCommand(request), CancellationToken.None);

        // Assert
        Assert.Same(expected, result);
        userServiceMock.Verify(s => s.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnServiceError_WhenCreateFails()
    {
        // Arrange
        var userServiceMock = new Mock<IUserService>();
        var request = new CreateUserRequest { UserName = "jane" };
        var expected = Result<string>.Error("Username already taken");
        userServiceMock.Setup(s => s.CreateAsync(request)).ReturnsAsync(expected);
        var handler = new CreateUserCommandHandler(userServiceMock.Object);

        // Act
        var result = await handler.Handle(new CreateUserCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Same(expected, result);
    }
}
