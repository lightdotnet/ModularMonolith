using Notifications.Tests.TestSupport;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using Xunit;

namespace Notifications.Tests.Application.Notifications.Commands;

public class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldMarkTheUsersUnreadEntryAsRead()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var entry = await host.AddAsync("user-1", NotificationStatus.None);
        var untouched = await host.AddAsync("user-1", NotificationStatus.None);
        var handler = new MarkNotificationReadCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MarkNotificationReadCommand("user-1", entry.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var statuses = await host.StatusesAsync();
        Assert.Equal(NotificationStatus.Read, statuses[entry.Id]);
        Assert.Equal(NotificationStatus.None, statuses[untouched.Id]);
    }

    [Fact]
    public async Task Handle_ShouldLeaveArchivedEntryArchived()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var entry = await host.AddAsync("user-1", NotificationStatus.Archived);
        var handler = new MarkNotificationReadCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MarkNotificationReadCommand("user-1", entry.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var statuses = await host.StatusesAsync();
        Assert.Equal(NotificationStatus.Archived, statuses[entry.Id]);
    }

    [Fact]
    public async Task Handle_ShouldBeNoOpSuccess_WhenEntryIsAddressedToAnotherUser()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var entry = await host.AddAsync("user-2", NotificationStatus.None);
        var handler = new MarkNotificationReadCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MarkNotificationReadCommand("user-1", entry.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var statuses = await host.StatusesAsync();
        Assert.Equal(NotificationStatus.None, statuses[entry.Id]);
    }

    [Fact]
    public async Task Handle_ShouldBeNoOpSuccess_WhenIdIsUnknown()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var handler = new MarkNotificationReadCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new MarkNotificationReadCommand("user-1", "missing"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
