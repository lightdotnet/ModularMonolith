using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Domain;
using Xunit;

namespace Notifications.Tests.Domain;

public class NotificationTests
{
    private static Notification CreateNotification() =>
        Notification.Create(
            "to-user",
            "Title",
            "Body",
            "/orders/1",
            "from-user",
            "Sender");

    [Fact]
    public void Create_ShouldSetAllFields_AndStartUnread()
    {
        // Act
        var notification = CreateNotification();

        // Assert
        Assert.Equal("from-user", notification.SenderUserId);
        Assert.Equal("Sender", notification.SenderName);
        Assert.Equal("to-user", notification.RecipientUserId);
        Assert.Equal("Title", notification.Title);
        Assert.Equal("Body", notification.Message);
        Assert.Equal("/orders/1", notification.Url);
        Assert.Equal(NotificationStatus.None, notification.Status);
    }

    [Fact]
    public void Create_ShouldAcceptNullOptionalFields()
    {
        // Act
        var notification = Notification.Create(
            "to-user",
            "Title",
            null,
            null,
            "from-user",
            null);

        // Assert
        Assert.Null(notification.SenderName);
        Assert.Null(notification.Message);
        Assert.Null(notification.Url);
        Assert.Equal(NotificationStatus.None, notification.Status);
    }

    [Fact]
    public void MarkAsRead_ShouldMoveStatusToRead_WhenUnread()
    {
        // Arrange
        var notification = CreateNotification();

        // Act
        notification.MarkAsRead();

        // Assert
        Assert.Equal(NotificationStatus.Read, notification.Status);
    }

    [Fact]
    public void MarkAsRead_ShouldBeIdempotent_WhenAlreadyRead()
    {
        // Arrange
        var notification = CreateNotification();
        notification.MarkAsRead();

        // Act
        notification.MarkAsRead();

        // Assert
        Assert.Equal(NotificationStatus.Read, notification.Status);
    }

    // The Archived -> Archived rule is not reachable through the public domain API (no transition
    // sets Archived); it is covered against a persisted row in NotificationServiceTests.
}
