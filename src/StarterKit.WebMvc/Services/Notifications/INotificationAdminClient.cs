using Light.Contracts;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Notifications;

/// <summary>
/// Notifications <c>NotificationController</c> (<c>notification</c>) — the admin surface. One
/// method per endpoint.
/// </summary>
public interface INotificationAdminClient
{
    /// <summary><c>GET notification</c></summary>
    Task<ApiResult<Paged<NotificationDto>>> SearchAsync(
        NotificationLookup lookup,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST notification?fromUserId=&amp;fromName=&amp;toUserId=</c></summary>
    Task<ApiResult> SendAsync(
        string fromUserId,
        string? fromName,
        string toUserId,
        SystemMessage message,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST notification/force_logout</c></summary>
    Task<ApiResult> ForceLogoutAsync(
        ForceLogoutMessage message,
        CancellationToken cancellationToken = default);
}
