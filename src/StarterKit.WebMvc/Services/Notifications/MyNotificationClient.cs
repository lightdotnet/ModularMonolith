using Light.Contracts;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Notifications;

internal sealed class MyNotificationClient(
    IHttpClientFactory httpClientFactory,
    ILogger<MyNotificationClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Notifications),
        logger),
    IMyNotificationClient
{
    public Task<ApiResult<Paged<NotificationDto>>> SearchAsync(
        NotificationLookup lookup,
        CancellationToken cancellationToken = default)
    {
        // The backend scopes this endpoint to the caller, so no recipient is ever sent.
        return SendAsync<Paged<NotificationDto>>(
            HttpMethod.Get,
            "user_notification",
            new ApiRequest
            {
                Query = NotificationQuery.From(
                    lookup,
                    includeRecipient: false),
            },
            cancellationToken);
    }

    public Task<ApiResult<NotificationDto>> GetAsync(
        string entryId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<NotificationDto>(
            HttpMethod.Get,
            $"user_notification/{Segment(entryId)}",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<int>> ReadAllAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<int>(
            HttpMethod.Post,
            "user_notification/read_all",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<int>> CountUnreadAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<int>(
            HttpMethod.Get,
            "user_notification/count_unread",
            cancellationToken: cancellationToken);
    }
}
