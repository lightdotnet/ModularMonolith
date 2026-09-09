namespace StarterKit.Notifications.Contracts.SystemNotifications;

public static class NotificationConstants
{
    // The name of the method that's used to send notification messages from the server to the client

    public const string ServerNotification = "server-notification";

    // The request path the real-time SignalR notification hub is mapped to

    public const string HubPath = "/signalr-hub";
}
