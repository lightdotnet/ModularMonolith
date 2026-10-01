using Light.Exceptions;
using StarterKit.Modules.Identity.Application.Authentication;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Infrastructure.Persistence;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Infrastructure.Authentication.Jwt;

internal class UserSessionService(
    JwtTokenIssuer tokenIssuer,
    IdentityDbContext context,
    IDateTime dateTime)
    : IUserSessionService
{
    public virtual async Task<TokenDto> GenerateTokenAsync(
        User user,
        DateTime tokenExpiresAt,
        DateTime refreshTokenExpiresAt,
        DeviceDto? device = null)
    {
        var now = dateTime.UtcNow;

        var newToken = new UserSession
        {
            UserId = user.Id,
            TokenExpiresAt = tokenExpiresAt,
            RefreshToken = JwtHelper.GenerateRefreshToken(),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            DeviceId = device?.Id,
            DeviceName = device?.Name,
            IpAddress = device?.IpAddress,
            PhysicalAddress = device?.PhysicalAddress,
        };

        var jwtToken = await tokenIssuer.IssueAsync(user, newToken.Id, tokenExpiresAt);

        newToken.Token = jwtToken;

        await context.UserSessions.AddAsync(newToken);
        await context.SaveChangesAsync();

        return new TokenDto(
            jwtToken,
            newToken.GetTokenExpiresInSeconds(now),
            newToken.RefreshToken);
    }

    public virtual async Task<TokenDto> RefreshTokenAsync(
        User user,
        string refreshToken,
        DateTime tokenExpiresAt,
        DateTime refreshTokenExpiresAt,
        DeviceDto? device = null)
    {
        var now = dateTime.UtcNow;

        // check refresh token is exist and not out of lifetime
        var userToken = await context.UserSessions
            .Where(x =>
                x.UserId == user.Id
                && x.RefreshToken == refreshToken
                && x.RefreshTokenExpiresAt >= now
                && x.Revoked == false)
            .FirstOrDefaultAsync()
            ?? throw new UnauthorizedException("Refresh token invalid.");

        var jwtToken = await tokenIssuer.IssueAsync(user, userToken.Id, tokenExpiresAt);

        userToken.Token = jwtToken;
        userToken.TokenExpiresAt = tokenExpiresAt;
        userToken.RefreshToken = JwtHelper.GenerateRefreshToken();
        userToken.RefreshTokenExpiresAt = refreshTokenExpiresAt;

        userToken.DeviceId = device?.Id;
        userToken.DeviceName = device?.Name;
        userToken.IpAddress = device?.IpAddress;
        userToken.PhysicalAddress = device?.PhysicalAddress;

        await context.SaveChangesAsync();

        return new TokenDto(
            jwtToken,
            userToken.GetTokenExpiresInSeconds(now),
            userToken.RefreshToken);
    }

    public async Task<IEnumerable<UserSessionDto>> GetUserTokensAsync(string userId)
    {
        var now = dateTime.UtcNow;

        var list = await context.UserSessions
            .Where(x =>
                x.UserId == userId
                &&
                    (x.TokenExpiresAt >= now
                    || (x.RefreshTokenExpiresAt.HasValue && x.RefreshTokenExpiresAt >= now))
                && x.Revoked == false)
            .AsNoTracking()
            .Select(s => new UserSessionDto
            {
                Id = s.Id,
                ExpiresAt = s.TokenExpiresAt,
                RefreshTokenExpiresAt = s.RefreshTokenExpiresAt,
                Device = new DeviceDto
                {
                    Id = s.DeviceId,
                    Name = s.DeviceName,
                    IpAddress = s.IpAddress,
                    PhysicalAddress = s.PhysicalAddress,
                },
            })
            .ToListAsync();

        return list;
    }

    public Task<bool> IsTokenValidAsync(string tokenId)
    {
        var now = dateTime.UtcNow;

        return context.UserSessions
            .Where(x =>
                x.Id == tokenId
                && x.Revoked == false
                && x.TokenExpiresAt > now)
            .AsNoTracking()
            .AnyAsync();
    }

    public Task RevokeAsync(string userId, string tokenId)
    {
        return context.UserSessions
            .Where(x => x.Id == tokenId && x.UserId == userId)
            .ExecuteUpdateAsync(e => e.SetProperty(p => p.Revoked, true));
    }
}
