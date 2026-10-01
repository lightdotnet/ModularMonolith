using Light.Contracts;
using Light.Mediator;
using Moq;
using StarterKit.Modules.Notifications.Api;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using StarterKit.Shared;
using Xunit;

namespace Notifications.Tests.Api;

public class NotificationsModuleApiTests
{
    [Fact]
    public async Task SendAsync_ShouldDispatchSendNotificationCommand_WithCurrentUserAsSender()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var message = new SystemMessage { Title = "Hello", Message = "Body", Url = "/x" };
        var ct = TestContext.Current.CancellationToken;
        var expected = new SendNotificationCommand(
            "to-user",
            message,
            "current-user-id");
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(x => x.UserId).Returns("current-user-id");
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var api = new NotificationsModuleApi(mediatorMock.Object, currentUserMock.Object);

        // Act
        await api.SendAsync(
            "to-user",
            message,
            ct);

        // Assert
        mediatorMock.Verify(
            m => m.Send(
                It.Is<IRequest<IResult>>(r => expected.Equals(r)),
                ct),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldDispatchCommandWithoutSender_WhenNoCurrentUser()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var message = new SystemMessage { Title = "Hello" };
        var ct = TestContext.Current.CancellationToken;
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.SetupGet(x => x.UserId).Returns((string?)null);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var api = new NotificationsModuleApi(mediatorMock.Object, currentUserMock.Object);

        // Act
        await api.SendAsync(
            "to-user",
            message,
            ct);

        // Assert
        mediatorMock.Verify(
            m => m.Send(
                It.Is<IRequest<IResult>>(r =>
                    r is SendNotificationCommand
                    && ((SendNotificationCommand)r).RecipientUserId == "to-user"
                    && ((SendNotificationCommand)r).SenderUserId == null),
                ct),
            Times.Once);
    }
}
