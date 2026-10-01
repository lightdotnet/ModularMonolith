using Light.Contracts;
using Light.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Endpoints;
using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using StarterKit.Modules.Notifications.Application.Notifications.Queries;
using StarterKit.Shared;
using Xunit;

namespace Notifications.Tests.Endpoints;

public class NotificationControllerTests
{
    private const string CurrentUserId = "current-user-id";

    private static (NotificationController Controller, Mock<IMediator> Mediator) CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(x => x.UserId).Returns(CurrentUserId);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton(mediatorMock.Object)
                .BuildServiceProvider(),
        };

        var controller = new NotificationController(currentUserMock.Object)
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
    public async Task SendToUserId_ShouldDispatchSendCommand_WithCurrentUserAsSender()
    {
        // Arrange
        var (controller, mediatorMock) = CreateSut();
        var message = new SystemMessage { Title = "Hello" };
        var expected = Result.Success();
        mediatorMock
            .Setup(m => m.Send(
                It.Is<SendNotificationCommand>(c =>
                    c.RecipientUserId == "to-user"
                    && c.SenderUserId == CurrentUserId
                    && c.Message == message),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var response = await controller.SendToUserId(
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
