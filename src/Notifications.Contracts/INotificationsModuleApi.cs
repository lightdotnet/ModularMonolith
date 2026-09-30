using StarterKit.Modules.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Modules.Notifications.Contracts;

/// <summary>
/// In-process seam through which other modules notify users through the Notifications module.
/// </summary>
public interface INotificationsModuleApi
{
    /// <summary>
    /// Persists a notification for the recipient, then pushes it live to that user over SignalR.
    /// </summary>
    Task SendAsync(
        string fromUserId,
        string? fromName,
        string toUserId,
        SystemMessage message,
        CancellationToken ct = default);
}
