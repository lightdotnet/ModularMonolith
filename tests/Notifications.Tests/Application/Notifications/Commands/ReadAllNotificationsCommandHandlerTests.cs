using Moq;
using StarterKit.Notifications.Api.Application.Notifications.Commands;
using StarterKit.Notifications.Contracts.Services;
using Xunit;

namespace Notifications.Tests.Application.Notifications.Commands;

public class ReadAllNotificationsCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReadAllForTheUserAndReturnTheCount()
    {
        // Arrange
        var service = new Mock<INotificationService>();
        service
            .Setup(x => x.ReadAllAsync("user-1"))
            .ReturnsAsync(3);

        var handler = new ReadAllNotificationsCommandHandler(service.Object);

        // Act
        var result = await handler.Handle(
            new ReadAllNotificationsCommand("user-1"),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data);
        service.Verify(
            x => x.ReadAllAsync("user-1"),
            Times.Once);
    }
}
