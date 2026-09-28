using Light.Contracts;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Notifications;

internal sealed class NotificationAdminClient(
    IHttpClientFactory httpClientFactory,
    ILogger<NotificationAdminClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Notifications),
        logger),
    INotificationAdminClient
{
    public Task<ApiResult<Paged<NotificationDto>>> SearchAsync(
        NotificationLookup lookup,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<Paged<NotificationDto>>(
            HttpMethod.Get,
            "notification",
            new ApiRequest { Query = NotificationQuery.From(lookup) },
            cancellationToken);
    }

    public Task<ApiResult> SendAsync(
        string fromUserId,
        string? fromName,
        string toUserId,
        SystemMessage message,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Post,
            "notification",
            new ApiRequest
            {
                Body = message,
                Query = new Dictionary<string, string?>
                {
                    ["fromUserId"] = fromUserId,
                    ["fromName"] = fromName,
                    ["toUserId"] = toUserId,
                },
            },
            cancellationToken);
    }

    public Task<ApiResult> ForceLogoutAsync(
        ForceLogoutMessage message,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Post,
            "notification/force_logout",
            new ApiRequest { Body = message },
            cancellationToken);
    }
}
