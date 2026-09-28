using Moq;
using Notifications.Tests.TestSupport;
using StarterKit.Notifications.Api.Services;
using StarterKit.Notifications.Api.SignalR;
using StarterKit.Notifications.Contracts.SystemNotifications;
using Xunit;

namespace Notifications.Tests.Services;

public class NotificationServiceReadAllTests
{
    [Fact]
    public async Task ReadAllAsync_ShouldMarkOnlyTheUsersUnreadEntriesAsRead()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var unread1 = await host.AddAsync("user-1", NotificationStatus.None);
        var unread2 = await host.AddAsync("user-1", NotificationStatus.None);
        var alreadyRead = await host.AddAsync("user-1", NotificationStatus.Read);
        var archived = await host.AddAsync("user-1", NotificationStatus.Archived);
        var otherUser = await host.AddAsync("user-2", NotificationStatus.None);

        var service = CreateService(host);

        // Act
        var count = await service.ReadAllAsync("user-1");

        // Assert
        Assert.Equal(2, count);

        var statuses = await host.StatusesAsync();
        Assert.Equal(NotificationStatus.Read, statuses[unread1.Id]);
        Assert.Equal(NotificationStatus.Read, statuses[unread2.Id]);
        Assert.Equal(NotificationStatus.Read, statuses[alreadyRead.Id]);
        Assert.Equal(NotificationStatus.Archived, statuses[archived.Id]);
        Assert.Equal(NotificationStatus.None, statuses[otherUser.Id]);
    }

    [Fact]
    public async Task ReadAllAsync_WithNothingUnread_ShouldReturnZero()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        await host.AddAsync("user-1", NotificationStatus.Read);
        await host.AddAsync("user-1", NotificationStatus.Archived);

        var service = CreateService(host);

        // Act
        var count = await service.ReadAllAsync("user-1");

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldLeaveUnreadCountAtZero()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        await host.AddAsync("user-1", NotificationStatus.None);
        await host.AddAsync("user-1", NotificationStatus.None);

        var service = CreateService(host);

        // Act
        await service.ReadAllAsync("user-1");

        // Assert
        Assert.Equal(0, await service.CountUnreadAsync("user-1"));
    }

    private static NotificationService CreateService(NotificationsTestHost host) =>
        new(
            host.Context,
            Mock.Of<IHubService>());
}
