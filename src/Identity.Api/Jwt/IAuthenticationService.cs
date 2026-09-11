using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.Jwt;

public interface IAuthenticationService
{
    Task<IResult<TokenDto>> GetTokenAsync(
        string username, string password,
        DeviceDto? device = null);

    Task<IResult<TokenDto>> RefreshTokenAsync(
        string accessToken, string refreshToken,
        DeviceDto? device = null);

    Task<IResult<HubTokenResponse>> IssueHubTokenAsync(
        string userId,
        string sessionId);

    /// <summary>
    /// Mints a full access+refresh token pair for a known user id, bypassing password/AD
    /// verification. Shared by <see cref="GetTokenAsync"/> (after password verification)
    /// and the external-login relay in <c>Identity.Web</c>, which has already resolved the
    /// user via <c>IExternalLoginService</c> and only needs the token-issuance tail.
    /// </summary>
    Task<IResult<TokenDto>> IssueTokenForUserAsync(
        string userId,
        DeviceDto? device = null);
}
