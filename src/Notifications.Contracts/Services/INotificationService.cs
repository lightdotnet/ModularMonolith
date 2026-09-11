using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Contracts.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetAsync(NotificationLookup request);

    Task<NotificationDto?> GetByIdAsync(string userId, string id);

    Task<int> CountUnreadAsync(string userId);

    Task MarkAsReadAsync(string userId, string id);

    Task ReadAllAsync(string userId);

    Task SaveAsync(string fromUserId, string? fromName, string toUserId, SystemMessage message);

    /// <summary>
    /// Persists a notification and pushes it live to the recipient over SignalR in one call —
    /// the cross-module equivalent of what <c>SendNotificationCommandHandler</c> does for the
    /// HTTP-facing admin endpoint. Intended for other modules (e.g. Approval) to notify a user
    /// in-process, via DI, without needing access to the internal SignalR hub service.
    /// </summary>
    Task SendAsync(
        string fromUserId,
        string? fromName,
        string toUserId,
        SystemMessage message,
        CancellationToken cancellationToken = default);
}
