using Notifications.Tests.TestSupport;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Application.Notifications.Queries;
using Xunit;

namespace Notifications.Tests.Application.Notifications.Queries;

public class CountUnreadNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCountOnlyTheUsersUnreadEntries()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        await host.AddAsync("user-1", NotificationStatus.None);
        await host.AddAsync("user-1", NotificationStatus.None);
        await host.AddAsync("user-1", NotificationStatus.Read);
        await host.AddAsync("user-1", NotificationStatus.Archived);
        await host.AddAsync("user-2", NotificationStatus.None);
        var handler = new CountUnreadNotificationsQueryHandler(host.Context);

        // Act
        var count = await handler.Handle(
            new CountUnreadNotificationsQuery("user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenTheUserHasNoEntries()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        await host.AddAsync("user-2", NotificationStatus.None);
        var handler = new CountUnreadNotificationsQueryHandler(host.Context);

        // Act
        var count = await handler.Handle(
            new CountUnreadNotificationsQuery("user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, count);
    }
}
