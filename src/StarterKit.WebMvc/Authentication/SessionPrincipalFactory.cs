using System.Security.Claims;
using StarterKit.Identity.Contracts;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Builds the cookie principal the same way the admin client builds its session
/// (<c>establish-session.ts</c> / <c>build-session-claims.ts</c>): every claim in the access token
/// (uid, un, jti, permission, role, employee_id, ...) plus the profile API's display fields and
/// raw claims — never the profile's identity (uid/un/jti/name identifier/name) or authorization
/// (role/permission) claims, which are JWT-only: a profile claim must not be able to shadow or add
/// to who the user is or what they may do.
/// </summary>
internal static class SessionPrincipalFactory
{
    private static readonly HashSet<string> JwtOnlyClaimTypes = new(StringComparer.Ordinal)
    {
        SessionClaimTypes.UserId,
        SessionClaimTypes.UserName,
        SessionClaimTypes.TokenId,
        SessionClaimTypes.Role,
        SessionClaimTypes.Permission,
        ClaimTypes.NameIdentifier,
        ClaimTypes.Name,
        ClaimTypes.Role,
    };

    public static ClaimsPrincipal Create(
        string accessToken,
        UserDto? profile,
        IEnumerable<Claim>? retainedClaims = null)
    {
        var claims = new List<Claim>(JwtClaimReader.Read(accessToken));

        if (profile is not null)
        {
            claims.AddRange(ProfileClaims(profile));
        }
        else if (retainedClaims is not null)
        {
            claims.AddRange(retainedClaims.Where(claim => !JwtOnlyClaimTypes.Contains(claim.Type)));
        }

        claims = Dedupe(claims);

        // Antiforgery binds its token to NameIdentifier when present; without it the token would
        // be bound to a hash of every claim and break whenever a refresh changes permissions.
        var userId = claims.Find(claim => claim.Type == SessionClaimTypes.UserId)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(
                ClaimTypes.NameIdentifier,
                userId));
        }

        var identity = new ClaimsIdentity(
            claims,
            SessionDefaults.AuthenticationScheme,
            SessionClaimTypes.UserName,
            SessionClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// The principal's claims that decoding <paramref name="accessToken"/> alone would not reproduce
    /// (i.e. profile-sourced) — carried over on refresh when the profile cannot be refetched, like
    /// the admin client's <c>extraClaims</c>.
    /// </summary>
    public static IReadOnlyList<Claim> NonTokenClaims(
        ClaimsPrincipal principal,
        string accessToken)
    {
        var tokenKeys = JwtClaimReader.Read(accessToken)
            .Select(Key)
            .ToHashSet(StringComparer.Ordinal);

        return principal.Claims
            .Where(claim => claim.Type != ClaimTypes.NameIdentifier && !tokenKeys.Contains(Key(claim)))
            .Select(claim => new Claim(
                claim.Type,
                claim.Value))
            .ToList();
    }

    private static IEnumerable<Claim> ProfileClaims(UserDto profile)
    {
        var fields = new (string Type, string? Value)[]
        {
            (SessionClaimTypes.FirstName, profile.FirstName),
            (SessionClaimTypes.LastName, profile.LastName),
            (SessionClaimTypes.Email, profile.Email),
            (SessionClaimTypes.PhoneNumber, profile.PhoneNumber),
            (SessionClaimTypes.AuthProvider, profile.AuthProvider),
        };

        foreach (var (type, value) in fields)
        {
            if (!string.IsNullOrEmpty(value))
            {
                yield return new Claim(
                    type,
                    value);
            }
        }

        foreach (var claim in profile.Claims)
        {
            if (!JwtOnlyClaimTypes.Contains(claim.Type) && !string.IsNullOrEmpty(claim.Value))
            {
                yield return new Claim(
                    claim.Type,
                    claim.Value);
            }
        }
    }

    private static List<Claim> Dedupe(IEnumerable<Claim> claims)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return claims
            .Where(claim => seen.Add(Key(claim)))
            .ToList();
    }

    private static string Key(Claim claim) => $"{claim.Type}:{claim.Value}";
}
