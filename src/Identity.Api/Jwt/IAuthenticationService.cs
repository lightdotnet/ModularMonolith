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
}
