using Moq;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using StarterKit.Modules.Notifications.Application.Common;
using Xunit;

namespace Notifications.Tests.Application.Notifications.Commands;

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
