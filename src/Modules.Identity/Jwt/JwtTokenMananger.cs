using Light.Exceptions;
using Microsoft.AspNetCore.Identity;
using Monolith.Identity.Data;
using Monolith.Identity.Services;
using System.Security.Claims;
using ClaimTypes = Monolith.Claims.ClaimTypes;

namespace Monolith.Identity.Jwt;

public class JwtTokenMananger(UserManager<User> userManager, IdentityDbContext context)
{
    public UserManager<User> UserManager => userManager;

    public virtual DateTimeOffset TimeNow => DateTimeOffset.Now;

    public virtual async Task<List<Claim>> GetUserClaimsAsync(User user)
    {
        var claims = await context.QueryUserClaims(user.Id).ToListAsync().ConfigureAwait(false);
        return [.. claims.Select(s => new Claim(s.Type, s.Value))];
    }

    public virtual async Task<TokenDto> GenerateTokenByAsync(
        User user,
        string issuer, string secretKey,
        DateTime tokenExpiresAt, DateTime refreshTokenExpiresAt,
        DeviceDto? device = null,
        bool saveToken = true)
    {
        var newToken = new JwtToken
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

        var claims = await GetUserClaimsAsync(user);

        // add TokenID
        claims.Add(new Claim(ClaimTypes.TokenId, newToken.Id));

        var jwtToken = JwtHelper.GenerateToken(
            issuer,
            claims,
            tokenExpiresAt,
            secretKey);

        if (saveToken is true)
        {
            newToken.Token = jwtToken;
        }

        await context.JwtTokens.AddAsync(newToken);
        await context.SaveChangesAsync();

        // *** note: must return jwtToken cause save token to DB is options
        return new TokenDto(jwtToken, newToken.TokenExpiresInSeconds, newToken.RefreshToken);
    }

    public virtual async Task<TokenDto> RefreshTokenAsync(
        User user,
        string refreshToken,
        string issuer, string secretKey,
        DateTime tokenExpiresAt, DateTime refreshTokenExpiresAt,
        string roleClaimType = ClaimTypes.Role, string userIdClaimType = ClaimTypes.UserId,
        DeviceDto? device = null,
        bool saveToken = true)
    {
        // check refresh token is exist and not out of lifetime
        var userToken = await context.JwtTokens
            .Where(x =>
                x.UserId == user.Id
                && x.RefreshToken == refreshToken
                && x.RefreshTokenExpiresAt >= TimeNow.Date
                && x.Revoked == false)
            .FirstOrDefaultAsync()
            ?? throw new UnauthorizedException("Refresh token invalid.");

        var claims = await GetUserClaimsAsync(user);

        // add TokenID
        claims.Add(new Claim(ClaimTypes.TokenId, userToken.Id));

        var timeNow = TimeNow.DateTime;

        var jwtToken = JwtHelper.GenerateToken(
            issuer,
            claims,
            tokenExpiresAt,
            secretKey);

        if (saveToken is true)
        {
            userToken.Token = jwtToken;
        }

        userToken.TokenExpiresAt = tokenExpiresAt;
        userToken.RefreshToken = JwtHelper.GenerateRefreshToken();
        userToken.RefreshTokenExpiresAt = refreshTokenExpiresAt;

        userToken.DeviceId = device?.Id;
        userToken.DeviceName = device?.Name;
        userToken.IpAddress = device?.IpAddress;
        userToken.PhysicalAddress = device?.PhysicalAddress;

        await context.SaveChangesAsync();

        // *** note: must return jwtToken cause save token to DB is options
        return new TokenDto(jwtToken, userToken.TokenExpiresInSeconds, userToken.RefreshToken);
    }

    public async Task<IEnumerable<UserTokenDto>> GetUserTokensAsync(string userId)
    {
        var now = TimeNow;

        var list = await context.JwtTokens
            .Where(x =>
                x.UserId == userId
                &&
                    (x.TokenExpiresAt >= now
                    || (x.RefreshTokenExpiresAt.HasValue && x.RefreshTokenExpiresAt >= now))
                && x.Revoked == false)
            .AsNoTracking()
            .Select(s => new UserTokenDto
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

    public Task<bool> IsTokenValidAsync(string accessToken)
    {
        return context.JwtTokens
            .Where(x =>
                x.Token == accessToken
                && x.Revoked == false
                && x.TokenExpiresAt > TimeNow)
            .AnyAsync();
    }

    public Task RevokedAsync(string userId, string tokenId)
    {
        return context.JwtTokens
            .Where(x => x.Id == tokenId && x.UserId == userId)
            .ExecuteUpdateAsync(e => e.SetProperty(p => p.Revoked, true));
    }
}
