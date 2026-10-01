using Notifications.Tests.TestSupport;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Application.Notifications.Queries;
using Xunit;

namespace Notifications.Tests.Application.Notifications.Queries;

public class GetNotificationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDto_WhenAddressedToTheUser()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var entry = await host.AddAsync(
            "user-1",
            NotificationStatus.None,
            "Hello",
            "Jane Doe");
        var handler = new GetNotificationQueryHandler(host.Context);

        // Act
        var dto = await handler.Handle(
            new GetNotificationQuery("user-1", entry.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entry.Id, dto.Id);
        Assert.Equal("sender", dto.SenderUserId);
        Assert.Equal("Jane Doe", dto.SenderName);
        Assert.Equal("user-1", dto.ToUserId);
        Assert.Equal("Hello", dto.Title);
        Assert.Equal(NotificationStatus.None, dto.Status);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAddressedToAnotherUser()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var entry = await host.AddAsync("user-2", NotificationStatus.None);
        var handler = new GetNotificationQueryHandler(host.Context);

        // Act
        var dto = await handler.Handle(
            new GetNotificationQuery("user-1", entry.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenIdIsUnknown()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var handler = new GetNotificationQueryHandler(host.Context);

        // Act
        var dto = await handler.Handle(
            new GetNotificationQuery("user-1", "missing"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(dto);
    }
}
