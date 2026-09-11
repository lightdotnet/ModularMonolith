using Microsoft.Extensions.Options;
using StarterKit.Identity.Api.Entities;
using StarterKit.Shared.Constants;
using System.Security.Claims;

namespace StarterKit.Identity.Api.Jwt;

/// <summary>
/// Issues the dedicated short-lived token the browser uses for the SignalR hub handshake.
/// Unlike <see cref="JwtTokenIssuer"/> it carries no role, permission or profile claims - only
/// the user id and the session (token) id, so <c>CloseOnAuthenticationExpiration</c> and session
/// revocation still apply - and it is stamped with a hub-only audience so it cannot be replayed
/// against the regular API.
/// </summary>
internal class HubTokenIssuer(
    JwtSigningService jwtSigningService,
    IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public string Issue(User user, string tokenId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypeConstants.UserId, user.Id),
            new(ClaimTypeConstants.TokenId, tokenId),
        };

        var expiresAt = DateTime.UtcNow.AddSeconds(_jwt.HubTokenExpirationSeconds);

        return jwtSigningService.Generate(claims, expiresAt, _jwt.HubAudience);
    }
}
