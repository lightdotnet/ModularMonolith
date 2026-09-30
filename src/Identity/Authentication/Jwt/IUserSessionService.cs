using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Models;

namespace StarterKit.Modules.Identity.Authentication.Jwt;

public interface IUserSessionService
{
    Task<TokenDto> GenerateTokenAsync(
        User user,
        DateTime tokenExpiresAt,
        DateTime refreshTokenExpiresAt,
        DeviceDto? device = null);

    Task<TokenDto> RefreshTokenAsync(
        User user,
        string refreshToken,
        DateTime tokenExpiresAt,
        DateTime refreshTokenExpiresAt,
        DeviceDto? device = null);

    Task<IEnumerable<UserSessionDto>> GetUserTokensAsync(string userId);

    Task<bool> IsTokenValidAsync(string tokenId);

    Task RevokeAsync(string userId, string tokenId);
}
