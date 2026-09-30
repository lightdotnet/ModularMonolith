using Light.Contracts;
using Light.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StarterKit.Modules.Identity.Authentication.Jwt;
using StarterKit.Modules.Identity.Endpoints;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Endpoints;

public class UserProfileControllerTests
{
    private static (Mock<ICurrentUser> CurrentUser, Mock<IUserService> UserService, Mock<IUserSessionService> SessionService) CreateMocks(
        string userId = "user-1", string? sessionId = "session-1")
    {
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(c => c.UserId).Returns(userId);
        currentUserMock.SetupGet(c => c.SessionId).Returns(sessionId);
        return (currentUserMock, new Mock<IUserService>(), new Mock<IUserSessionService>());
    }

    private static UserProfileController CreateController(
        Mock<ICurrentUser> currentUser, Mock<IUserService> userService, Mock<IUserSessionService> sessionService) =>
        new(currentUser.Object, userService.Object, sessionService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    [Fact]
    public void Constructor_ShouldThrowUnauthorized_WhenCurrentUserHasNoUserId()
    {
        // Arrange
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(c => c.UserId).Returns((string?)null);
        var userServiceMock = new Mock<IUserService>();
        var sessionServiceMock = new Mock<IUserSessionService>();

        // Act & Assert
        Assert.Throws<UnauthorizedException>(() =>
            new UserProfileController(currentUserMock.Object, userServiceMock.Object, sessionServiceMock.Object));
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenSessionIdIsMissing()
    {
        // Arrange
        var (currentUserMock, userServiceMock, sessionServiceMock) = CreateMocks(sessionId: null);
        var controller = CreateController(currentUserMock, userServiceMock, sessionServiceMock);

        // Act
        var response = await controller.Get();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsAssignableFrom<Light.Contracts.IResult>(objectResult.Value);
        Assert.False(result.IsSuccess);
        sessionServiceMock.Verify(s => s.IsTokenValidAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenSessionIsInvalid()
    {
        // Arrange
        var (currentUserMock, userServiceMock, sessionServiceMock) = CreateMocks();
        sessionServiceMock.Setup(s => s.IsTokenValidAsync("session-1")).ReturnsAsync(false);
        var controller = CreateController(currentUserMock, userServiceMock, sessionServiceMock);

        // Act
        var response = await controller.Get();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsAssignableFrom<Light.Contracts.IResult>(objectResult.Value);
        Assert.False(result.IsSuccess);
        userServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenProfileIsNotActive()
    {
        // Arrange
        var (currentUserMock, userServiceMock, sessionServiceMock) = CreateMocks();
        sessionServiceMock.Setup(s => s.IsTokenValidAsync("session-1")).ReturnsAsync(true);
        userServiceMock
            .Setup(s => s.GetByIdAsync("user-1"))
            .ReturnsAsync(Result<UserDto>.Success(new UserDto { Id = "user-1", UserName = "jane", Status = "Locked" }));
        var controller = CreateController(currentUserMock, userServiceMock, sessionServiceMock);

        // Act
        var response = await controller.Get();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsAssignableFrom<Light.Contracts.IResult>(objectResult.Value);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Get_ShouldReturnProfile_WhenSessionValidAndProfileActive()
    {
        // Arrange
        var (currentUserMock, userServiceMock, sessionServiceMock) = CreateMocks();
        sessionServiceMock.Setup(s => s.IsTokenValidAsync("session-1")).ReturnsAsync(true);
        var expected = Result<UserDto>.Success(new UserDto { Id = "user-1", UserName = "jane", Status = "Active" });
        userServiceMock.Setup(s => s.GetByIdAsync("user-1")).ReturnsAsync(expected);
        var controller = CreateController(currentUserMock, userServiceMock, sessionServiceMock);

        // Act
        var response = await controller.Get();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetTokens_ShouldDelegateToSessionService()
    {
        // Arrange: plain IEnumerable<UserSessionDto> is not a ResultBase, so ApiControllerBase.Ok
        // wraps it in a Result<IEnumerable<UserSessionDto>> before returning it.
        var (currentUserMock, userServiceMock, sessionServiceMock) = CreateMocks();
        var expected = new List<UserSessionDto> { new() { Id = "session-1" } };
        sessionServiceMock.Setup(s => s.GetUserTokensAsync("user-1")).ReturnsAsync(expected);
        var controller = CreateController(currentUserMock, userServiceMock, sessionServiceMock);

        // Act
        var response = await controller.GetTokens();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var wrapped = Assert.IsType<Result<IEnumerable<UserSessionDto>>>(objectResult.Value);
        Assert.Same(expected, wrapped.Data);
    }

    [Fact]
    public async Task RevokeToken_ShouldDelegateToSessionService_ForCurrentUser()
    {
        // Arrange
        var (currentUserMock, userServiceMock, sessionServiceMock) = CreateMocks();
        var controller = CreateController(currentUserMock, userServiceMock, sessionServiceMock);

        // Act
        await controller.RevokeToken("session-1");

        // Assert
        sessionServiceMock.Verify(s => s.RevokeAsync("user-1", "session-1"), Times.Once);
    }
}
