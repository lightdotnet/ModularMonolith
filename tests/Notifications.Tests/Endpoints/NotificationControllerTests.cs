using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Endpoints;
using StarterKit.Modules.Notifications.Features.Notifications.Commands;
using StarterKit.Modules.Notifications.Features.Notifications.Queries;
using Xunit;

namespace Notifications.Tests.Endpoints;

public class NotificationControllerTests
{
    private static (NotificationController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider(),
        };

        var controller = new NotificationController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
        };
        return (controller, mediatorMock);
    }

    [Fact]
    public async Task GetAsync_ShouldDispatchSearchQuery_WithTheLookup()
    {
        // Arrange: PagedResult<T> derives from ResultBase, so Ok(...) passes it through unwrapped.
        var (controller, mediatorMock) = CreateSut();
        var lookup = new NotificationLookup { ToUserId = "user-1" };
        var expected = new PagedResult<NotificationDto>([], 1, 20, 0);
        mediatorMock
            .Setup(m => m.Send(It.Is<SearchNotificationsQuery>(q => q.Lookup == lookup), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.GetAsync(lookup);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task SendToUserId_ShouldDispatchSendCommand()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var message = new SystemMessage { Title = "Hello" };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<SendNotificationCommand>(c =>
                    c.FromUserId == "from-user"
                    && c.FromName == "Sender"
                    && c.ToUserId == "to-user"
                    && c.Message == message),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SendToUserId(
            "from-user",
            "Sender",
            "to-user",
            message);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }

    [Fact]
    public async Task ForceLogout_ShouldDispatchForceLogoutCommand()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var message = new ForceLogoutMessage("user-1");
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(It.Is<ForceLogoutCommand>(c => c.Message == message), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.ForceLogout(message);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Same(expected, objectResult.Value);
    }
}
