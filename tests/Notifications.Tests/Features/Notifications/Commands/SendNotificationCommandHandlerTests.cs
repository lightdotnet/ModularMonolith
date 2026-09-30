using Microsoft.EntityFrameworkCore;
using Moq;
using Notifications.Tests.TestSupport;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Features.Notifications.Commands;
using Xunit;

namespace Notifications.Tests.Features.Notifications.Commands;

public class SendNotificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPersistUnreadEntry_WithAuditFields()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        host.CurrentUser.UserId = "actor";
        var auditTime = host.DateTime.UtcNow;
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object);
        var message = new SystemMessage { Title = "Hello", Message = "Body", Url = "/x" };
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await handler.Handle(
            new SendNotificationCommand(
                "from-user",
                "Sender",
                "to-user",
                message),
            ct);

        // Assert
        Assert.True(result.IsSuccess);

        var saved = await host.Context.Notifications
            .AsNoTracking()
            .SingleAsync(ct);
        Assert.Equal("from-user", saved.FromUserId);
        Assert.Equal("Sender", saved.FromName);
        Assert.Equal("to-user", saved.ToUserId);
        Assert.Equal("Hello", saved.Title);
        Assert.Equal("Body", saved.Message);
        Assert.Equal("/x", saved.Url);
        Assert.Equal(NotificationStatus.None, saved.Status);
        Assert.Equal("actor", saved.CreatedBy);

        // Sqlite stores DateTimeOffset as Unix seconds.
        Assert.Equal(auditTime.ToUnixTimeSeconds(), saved.Created.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task Handle_ShouldPushToRecipient_AfterTheEntryIsSaved()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object);
        var message = new SystemMessage { Title = "Hello" };
        var ct = TestContext.Current.CancellationToken;
        var persistedAtPush = -1;
        host.Hub
            .Setup(h => h.SendAsync(message, "to-user", ct))
            .Callback(() => persistedAtPush = host.Context.Notifications.AsNoTracking().Count())
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(
            new SendNotificationCommand(
                "from-user",
                null,
                "to-user",
                message),
            ct);

        // Assert
        host.Hub.Verify(h => h.SendAsync(message, "to-user", ct), Times.Once);
        Assert.Equal(1, persistedAtPush);
    }
}
