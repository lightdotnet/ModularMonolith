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
    /// <remarks>
    /// The sender is the current user of the calling scope; a background caller with no
    /// signed-in user produces a notification without a sender.
    /// </remarks>
    Task SendAsync(
        string toUserId,
        SystemMessage message,
        CancellationToken ct = default);
}
