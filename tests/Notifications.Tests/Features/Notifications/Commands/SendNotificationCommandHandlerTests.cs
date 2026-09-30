using Microsoft.EntityFrameworkCore;
using Moq;
using Notifications.Tests.TestSupport;
using StarterKit.Modules.Identity.Contracts;
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
        host.IdentityApi
            .Setup(x => x.GetUserAsync("from-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary("from-user", "sender", "sender@example.com", "Sender", null, true));
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object, host.IdentityApi.Object);
        var message = new SystemMessage { Title = "Hello", Message = "Body", Url = "/x" };
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await handler.Handle(
            new SendNotificationCommand(
                "to-user",
                message,
                "from-user"),
            ct);

        // Assert
        Assert.True(result.IsSuccess);

        var saved = await host.Context.Notifications
            .AsNoTracking()
            .SingleAsync(ct);
        Assert.Equal("from-user", saved.SenderUserId);
        Assert.Equal("Sender", saved.SenderName);
        Assert.Equal("to-user", saved.RecipientUserId);
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
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object, host.IdentityApi.Object);
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
                "to-user",
                message,
                null),
            ct);

        // Assert
        host.Hub.Verify(h => h.SendAsync(message, "to-user", ct), Times.Once);
        Assert.Equal(1, persistedAtPush);
    }

    [Fact]
    public async Task Handle_ShouldNotLookUpSender_WhenSenderUserIdIsNull()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object, host.IdentityApi.Object);
        var message = new SystemMessage { Title = "Hello" };
        var ct = TestContext.Current.CancellationToken;

        // Act
        var result = await handler.Handle(
            new SendNotificationCommand(
                "to-user",
                message,
                null),
            ct);

        // Assert
        Assert.True(result.IsSuccess);
        host.IdentityApi.Verify(
            x => x.GetUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var saved = await host.Context.Notifications
            .AsNoTracking()
            .SingleAsync(ct);
        Assert.Null(saved.SenderUserId);
        Assert.Null(saved.SenderName);
    }

    [Fact]
    public async Task Handle_ShouldStoreSenderIdWithoutName_WhenSenderIsNotFound()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        host.IdentityApi
            .Setup(x => x.GetUserAsync("ghost-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummary?)null);
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object, host.IdentityApi.Object);
        var message = new SystemMessage { Title = "Hello" };
        var ct = TestContext.Current.CancellationToken;

        // Act
        await handler.Handle(
            new SendNotificationCommand(
                "to-user",
                message,
                "ghost-user"),
            ct);

        // Assert
        var saved = await host.Context.Notifications
            .AsNoTracking()
            .SingleAsync(ct);
        Assert.Equal("ghost-user", saved.SenderUserId);
        Assert.Null(saved.SenderName);
    }

    [Fact]
    public async Task Handle_ShouldUseFirstAndLastName_AsSenderName()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        host.IdentityApi
            .Setup(x => x.GetUserAsync("jane-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary("jane-id", "jane", "jane@example.com", "Jane", "Doe", true));
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object, host.IdentityApi.Object);
        var message = new SystemMessage { Title = "Hello" };
        var ct = TestContext.Current.CancellationToken;

        // Act
        await handler.Handle(
            new SendNotificationCommand(
                "to-user",
                message,
                "jane-id"),
            ct);

        // Assert
        var saved = await host.Context.Notifications
            .AsNoTracking()
            .SingleAsync(ct);
        Assert.Equal("Jane Doe", saved.SenderName);
    }

    [Fact]
    public async Task Handle_ShouldFallBackToUserName_WhenSenderHasNoFirstOrLastName()
    {
        // Arrange
        using var host = new NotificationsTestHost();
        host.IdentityApi
            .Setup(x => x.GetUserAsync("anon-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSummary("anon-id", "anon.user", "anon@example.com", null, null, true));
        var handler = new SendNotificationCommandHandler(host.Context, host.Hub.Object, host.IdentityApi.Object);
        var message = new SystemMessage { Title = "Hello" };
        var ct = TestContext.Current.CancellationToken;

        // Act
        await handler.Handle(
            new SendNotificationCommand(
                "to-user",
                message,
                "anon-id"),
            ct);

        // Assert
        var saved = await host.Context.Notifications
            .AsNoTracking()
            .SingleAsync(ct);
        Assert.Equal("anon.user", saved.SenderName);
    }
}
