using Light.Contracts;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Notifications;

/// <summary>
/// Notifications <c>UserNotificationController</c> (<c>user_notification</c>) — the signed-in
/// user's own inbox. One method per endpoint.
/// </summary>
public interface IMyNotificationClient
{
    /// <summary><c>GET user_notification</c> — the backend forces <c>toUserId</c> to the caller.</summary>
    Task<ApiResult<Paged<NotificationDto>>> SearchAsync(
        NotificationLookup lookup,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user_notification/{entryId}</c> — also marks the entry read.</summary>
    Task<ApiResult<NotificationDto>> GetAsync(
        string entryId,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST user_notification/read_all</c> — marks every unread entry read; returns how many were marked.</summary>
    Task<ApiResult<int>> ReadAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user_notification/count_unread</c></summary>
    Task<ApiResult<int>> CountUnreadAsync(
        CancellationToken cancellationToken = default);
}
