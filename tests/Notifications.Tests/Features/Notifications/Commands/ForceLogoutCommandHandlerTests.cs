using Moq;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Features.Notifications.Commands;
using StarterKit.Modules.Notifications.SignalR;
using Xunit;

namespace Notifications.Tests.Features.Notifications.Commands;

public class ForceLogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPushMessageToTheTargetUser_AndSucceed()
    {
        // Arrange
        var hubMock = new Mock<IHubService>();
        var handler = new ForceLogoutCommandHandler(hubMock.Object);
        var message = new ForceLogoutMessage("user-1");
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await handler.Handle(new ForceLogoutCommand(message), ct);

        // Assert
        Assert.True(result.IsSuccess);
        hubMock.Verify(h => h.SendAsync(message, "user-1", ct), Times.Once);
    }
}
