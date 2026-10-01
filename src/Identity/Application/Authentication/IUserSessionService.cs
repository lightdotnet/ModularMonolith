using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Domain;

namespace StarterKit.Modules.Identity.Application.Authentication;

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
