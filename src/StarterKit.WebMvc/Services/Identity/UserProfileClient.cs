using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

internal sealed class UserProfileClient(
    IHttpClientFactory httpClientFactory,
    ILogger<UserProfileClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Identity),
        logger),
    IUserProfileClient
{
    public Task<ApiResult<UserDto>> GetAsync(
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserDto>(
            HttpMethod.Get,
            "user_profile",
            new ApiRequest
            {
                AccessToken = accessToken,
                ThrowOnAuthFailure = accessToken is null,
            },
            cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<UserSessionDto>>> GetSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<UserSessionDto>>(
            HttpMethod.Get,
            "user_profile/token/list",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult> RevokeSessionAsync(
        string tokenId,
        CancellationToken cancellationToken = default)
    {
        // The endpoint binds [FromBody] string, so the body is a bare JSON string.
        return SendAsync(
            HttpMethod.Put,
            "user_profile/token/revoke",
            new ApiRequest { Body = tokenId },
            cancellationToken);
    }
}
