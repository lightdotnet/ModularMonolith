using Light.Contracts;
using Light.Mediator;
using Moq;
using StarterKit.Modules.Notifications.Api;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Features.Notifications.Commands;
using Xunit;

namespace Notifications.Tests.Api;

public class NotificationsModuleApiTests
{
    [Fact]
    public async Task SendAsync_ShouldDispatchSendNotificationCommand()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var message = new SystemMessage { Title = "Hello", Message = "Body", Url = "/x" };
        var ct = TestContext.Current.CancellationToken;
        var expected = new SendNotificationCommand(
            "from-user",
            "Sender",
            "to-user",
            message);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<IResult>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var api = new NotificationsModuleApi(mediatorMock.Object);

        // Act
        await api.SendAsync(
            "from-user",
            "Sender",
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
}
