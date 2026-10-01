using Notifications.Tests.TestSupport;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Application.Notifications.Queries;
using Xunit;

namespace Notifications.Tests.Application.Notifications.Queries;

public class SearchNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldFilterByRecipientAndStatus_NewestFirst()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var older = await host.AddAsync("user-1", NotificationStatus.None, "older");
        await host.AddAsync("user-1", NotificationStatus.Read, "read");
        await host.AddAsync("user-2", NotificationStatus.None, "other user");
        var newer = await host.AddAsync("user-1", NotificationStatus.None, "newer");
        var handler = new SearchNotificationsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchNotificationsQuery(
                new NotificationLookup { ToUserId = "user-1", Status = NotificationStatus.None }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Data.TotalRecords);
        Assert.Equal(
            new[] { newer.Id, older.Id },
            result.Data.Records.Select(x => x.Id));
    }

    [Fact]
    public async Task Handle_ShouldReturnEveryEntry_WhenNoFilterIsSet()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        await host.AddAsync("user-1", NotificationStatus.None);
        await host.AddAsync("user-1", NotificationStatus.Read);
        await host.AddAsync("user-2", NotificationStatus.Archived);
        var handler = new SearchNotificationsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchNotificationsQuery(new NotificationLookup()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Data.TotalRecords);
    }

    [Fact]
    public async Task Handle_ShouldPage()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        await host.AddAsync("user-1", NotificationStatus.None, "first");
        var second = await host.AddAsync("user-1", NotificationStatus.None, "second");
        await host.AddAsync("user-1", NotificationStatus.None, "third");
        var handler = new SearchNotificationsQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SearchNotificationsQuery(new NotificationLookup { PageNumber = 2, PageSize = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Data.TotalRecords);
        Assert.Equal(second.Id, Assert.Single(result.Data.Records).Id);
    }
}
