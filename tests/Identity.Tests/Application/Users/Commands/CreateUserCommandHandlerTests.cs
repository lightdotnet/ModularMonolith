using Light.Contracts;
using Light.Mediator;
using Moq;
using StarterKit.Identity.Api.Application.Users.Commands;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Identity.Contracts.Users;
using Xunit;

namespace Identity.Tests.Application.Users.Commands;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPublishUserCreatedIntegrationEvent_WhenCreateSucceeds()
    {
        // Arrange
        var userServiceMock = new Mock<IUserService>();
        var publisherMock = new Mock<IPublisher>();
        var request = new CreateUserRequest { UserName = "jane", Email = "jane@example.com" };
        userServiceMock
            .Setup(s => s.CreateAsync(request))
            .ReturnsAsync(Result<string>.Success("user-1"));
        var handler = new CreateUserCommandHandler(userServiceMock.Object, publisherMock.Object);

        // Act
        var result = await handler.Handle(new CreateUserCommand(request), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("user-1", result.Data);
        publisherMock.Verify(
            p => p.Publish(
                It.Is<UserCreatedIntegrationEvent>(e =>
                    e.UserId == "user-1" && e.UserName == "jane" && e.Email == "jane@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotPublish_WhenCreateFails()
    {
        // Arrange
        var userServiceMock = new Mock<IUserService>();
        var publisherMock = new Mock<IPublisher>();
        var request = new CreateUserRequest { UserName = "jane" };
        userServiceMock
            .Setup(s => s.CreateAsync(request))
            .ReturnsAsync(Result<string>.Error("Username already taken"));
        var handler = new CreateUserCommandHandler(userServiceMock.Object, publisherMock.Object);

        // Act
        var result = await handler.Handle(new CreateUserCommand(request), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        publisherMock.Verify(
            p => p.Publish(It.IsAny<UserCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
