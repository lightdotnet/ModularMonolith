using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Identity.Api.Application.Roles.Commands;
using StarterKit.Identity.Api.Controllers;
using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using Xunit;

namespace Identity.Tests.Controllers;

public class RoleControllerTests
{
    private static (RoleController Controller, Mock<IRoleService> RoleService, Mock<IMediator> Mediator) CreateSut()
    {
        var roleServiceMock = new Mock<IRoleService>();
        var mediatorMock = new Mock<IMediator>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider(),
        };

        var controller = new RoleController(roleServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, roleServiceMock, mediatorMock);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnAllRoles()
    {
        // Arrange: plain IEnumerable<RoleDto> is not a ResultBase, so ApiControllerBase.Ok wraps it
        // in a Result<IEnumerable<RoleDto>> before returning it.
        var (controller, roleServiceMock, _) = CreateSut();
        var expected = new List<RoleDto> { new() { Id = "1", Name = "Admin" } };
        roleServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        var wrapped = Assert.IsType<Result<IEnumerable<RoleDto>>>(objectResult.Value);
        Assert.Same(expected, wrapped.Data);
    }

    [Fact]
    public async Task GetAsync_ById_ShouldReturnServiceResult()
    {
        // Arrange
        var (controller, roleServiceMock, _) = CreateSut();
        var expected = Result<RoleDto>.NotFound("Role missing not found");
        roleServiceMock.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync("missing");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task CreateAsync_ShouldDispatchCommand()
    {
        // Arrange
        var (controller, _, mediatorMock) = CreateSut();
        var request = new CreateRoleRequest { Name = "Admin" };
        var expected = Result<string>.Success("role-1");
        mediatorMock
            .Setup(m => m.Send(It.Is<CreateRoleCommand>(c => c.Model == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.CreateAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task UpdateAsync_ShouldDispatchCommand()
    {
        // Arrange
        var (controller, _, mediatorMock) = CreateSut();
        var request = new RoleDto { Id = "role-1", Name = "Admin" };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<UpdateRoleCommand>(c => c.Model == request), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.UpdateAsync(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDispatchCommand()
    {
        // Arrange
        var (controller, _, mediatorMock) = CreateSut();
        var expected = Result.Error("Role has already setup claims.");
        mediatorMock
            .Setup(m => m.Send(It.Is<DeleteRoleCommand>(c => c.Id == "role-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.DeleteAsync("role-1");

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
