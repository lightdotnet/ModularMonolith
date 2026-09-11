namespace StarterKit.Notifications.Contracts.SystemNotifications;

/// <summary>
/// Configurable location of the real-time SignalR notification hub. Bound from the
/// <c>Notifications:Hub</c> configuration section; defaults to <c>/signalr-hub</c>.
/// </summary>
public sealed class NotificationHubOptions
{
    public const string SectionName = "Notifications:Hub";

    /// <summary>Request path the SignalR notification hub endpoint is mapped to.</summary>
    public string Path { get; set; } = "/signalr-hub";
}
