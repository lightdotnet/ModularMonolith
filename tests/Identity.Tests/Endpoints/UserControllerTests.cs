using Light.ActiveDirectory.Interfaces;
using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Users.Commands;
using StarterKit.Modules.Identity.Application.Users.Queries;
using StarterKit.Modules.Identity.Application.Users.Services;
using StarterKit.Modules.Identity.Endpoints;
using Xunit;

namespace Identity.Tests.Endpoints;

public class UserControllerTests
{
    private static (
        UserController Controller,
        Mock<IUserService> UserService,
        Mock<IActiveDirectoryService> ActiveDirectory,
        Mock<IMediator> Mediator,
        Mock<IUserQueryService> UserQuery) CreateSut()
    {
        var userServiceMock = new Mock<IUserService>();
        var userQueryMock = new Mock<IUserQueryService>();
        var adServiceMock = new Mock<IActiveDirectoryService>();
        var mediatorMock = new Mock<IMediator>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider(),
        };

        var controller = new UserController(
            userServiceMock.Object,
            userQueryMock.Object,
            adServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, userServiceMock, adServiceMock, mediatorMock, userQueryMock);
    }

    [Fact]
    public async Task SearchAsync_ShouldDispatchQuery()
    {
        // Arrange: PagedResult<T> derives from ResultBase, so Ok(...) passes it through unwrapped.
        var (controller, _, _, mediatorMock, _) = CreateSut();
        var request = new SearchUserRequest { SearchValue = "jane", PageNumber = 2, PageSize = 5 };
        var expected = new PagedResult<UserDto>([], 2, 5, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchUserQuery>(q => q.Model == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SearchAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnAllUsers()
    {
        // Arrange: plain IReadOnlyList<UserDto> is not a ResultBase, so ApiControllerBase.Ok wraps it
        // in a Result<IReadOnlyList<UserDto>> before returning it.
        var (controller, _, _, _, userQueryMock) = CreateSut();
        var expected = new List<UserDto> { new() { Id = "1", UserName = "jane" } };
        userQueryMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var wrapped = Assert.IsType<Result<IReadOnlyList<UserDto>>>(objectResult.Value);
        Assert.Same(expected, wrapped.Data);
    }

    [Fact]
    public async Task GetAsync_ById_ShouldReturnServiceResult()
    {
        // Arrange
        var (controller, userServiceMock, _, _, _) = CreateSut();
        var expected = Result<UserDto>.NotFound("User missing not found");
        userServiceMock.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync("missing");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnServiceResult()
    {
        // Arrange
        var (controller, userServiceMock, _, _, _) = CreateSut();
        var expected = Result<UserDto>.Success(new UserDto { Id = "1", UserName = "jane" });
        userServiceMock.Setup(s => s.GetByUserNameAsync("jane")).ReturnsAsync(expected);

        // Act
        var response = await controller.GetByUsernameAsync("jane");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnError_WhenRouteAndBodyIdsDoNotMatch()
    {
        // Arrange
        var (controller, _, _, mediatorMock, _) = CreateSut();
        var request = new UserDto { Id = "other-id", UserName = "jane" };

        // Act
        var response = await controller.PutAsync("route-id", request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var result = Assert.IsType<Result>(objectResult.Value);
        Assert.False(result.IsSuccess);
        mediatorMock.Verify(
            m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PutAsync_ShouldDispatchCommand_WhenIdsMatch()
    {
        // Arrange
        var (controller, _, _, mediatorMock, _) = CreateSut();
        var request = new UserDto { Id = "user-1", UserName = "jane" };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<UpdateUserCommand>(c => c.Model == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.PutAsync("user-1", request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDispatchCommand()
    {
        // Arrange
        var (controller, _, _, mediatorMock, _) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<DeleteUserCommand>(c => c.Id == "user-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.DeleteAsync("user-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task ForcePasswordAsync_ShouldDispatchCommand()
    {
        // Arrange
        var (controller, _, _, mediatorMock, _) = CreateSut();
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<ForcePasswordCommand>(c => c.Id == "user-1" && c.Password == "new-pass"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.ForcePasswordAsync("user-1", "new-pass");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
