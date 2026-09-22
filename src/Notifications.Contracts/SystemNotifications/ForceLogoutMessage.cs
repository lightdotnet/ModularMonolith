namespace StarterKit.Notifications.Contracts.SystemNotifications;

public record ForceLogoutMessage(string UserId) : INotificationMessage;
