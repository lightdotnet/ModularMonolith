using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

internal sealed class AuthClient(
    IHttpClientFactory httpClientFactory,
    ILogger<AuthClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Identity),
        logger),
    IAuthClient
{
    public Task<ApiResult<TokenDto>> GetTokenAsync(
        GetTokenRequest request,
        string? deviceId = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TokenDto>(
            HttpMethod.Post,
            "auth/token/get",
            new ApiRequest
            {
                Body = request,
                Query = new Dictionary<string, string?>
                {
                    ["deviceId"] = deviceId,
                    ["deviceName"] = deviceName,
                },
                Anonymous = true,
                ThrowOnAuthFailure = false,
            },
            cancellationToken);
    }

    public Task<ApiResult<TokenDto>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TokenDto>(
            HttpMethod.Post,
            "auth/token/refresh",
            new ApiRequest
            {
                Body = request,
                Anonymous = true,
                ThrowOnAuthFailure = false,
            },
            cancellationToken);
    }

    public Task<ApiResult<TokenDto>> ExchangeExternalCodeAsync(
        ExchangeAuthCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TokenDto>(
            HttpMethod.Post,
            "auth/token/external",
            new ApiRequest
            {
                Body = request,
                Anonymous = true,
                ThrowOnAuthFailure = false,
            },
            cancellationToken);
    }

    public Task<ApiResult<HubTokenResponse>> GetHubTokenAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<HubTokenResponse>(
            HttpMethod.Post,
            "auth/token/hub",
            cancellationToken: cancellationToken);
    }
}
