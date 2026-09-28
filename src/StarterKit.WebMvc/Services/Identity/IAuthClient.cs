using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

/// <summary>
/// Identity <c>TokenController</c> (<c>auth</c>) — one method per endpoint.
/// </summary>
public interface IAuthClient
{
    /// <summary><c>POST auth/token/get</c> — password or Active Directory sign-in (the backend picks the provider from the user record).</summary>
    Task<ApiResult<TokenDto>> GetTokenAsync(
        GetTokenRequest request,
        string? deviceId = null,
        string? deviceName = null,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST auth/token/refresh</c> — rotates the access/refresh token pair.</summary>
    Task<ApiResult<TokenDto>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST auth/token/external</c> — redeems the one-time PKCE code from the Microsoft external-login relay.</summary>
    Task<ApiResult<TokenDto>> ExchangeExternalCodeAsync(
        ExchangeAuthCodeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST auth/token/hub</c> — mints a short-lived, hub-audience-only SignalR token for the current session.</summary>
    Task<ApiResult<HubTokenResponse>> GetHubTokenAsync(
        CancellationToken cancellationToken = default);
}
