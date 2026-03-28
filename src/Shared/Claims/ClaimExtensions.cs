using System.Security.Claims;

namespace Monolith.Claims;

public static class ClaimExtensions
{
    public static List<Claim> Add(this List<Claim> claims, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            claims.Add(new Claim(key, value));
        }

        return claims;
    }
}
