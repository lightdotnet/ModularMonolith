using StarterKit.Shared.Constants;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace StarterKit.Modules.Identity.Infrastructure.Authentication.Jwt;

internal static class JwtHelper
{
    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// return Claim to use ClaimsIdentity
    /// </summary>
    public static List<Claim> ReadClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(ParseBase64WithoutPadding(payload));

        if (keyValuePairs is null)
            return [];

        var claims = new List<Claim>();

        AddMultiValueClaims(claims, keyValuePairs, ClaimTypeConstants.Role);

        AddMultiValueClaims(claims, keyValuePairs, ClaimTypeConstants.Permission);

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(
            kvp.Key,
            kvp.Value.ToString() ?? string.Empty)));

        return claims;
    }

    private static void AddMultiValueClaims(
        List<Claim> claims,
        Dictionary<string, object> source,
        string claimType)
    {
        if (!source.Remove(claimType, out var value) || value?.ToString() is not { Length: > 0 } stringValue)
            return;

        if (stringValue.Trim().StartsWith('['))
        {
            var values = JsonSerializer.Deserialize<string[]>(stringValue);

            if (values is not null)
                claims.AddRange(values.Select(v => new Claim(claimType, v)));
        }
        else
        {
            claims.Add(new Claim(claimType, stringValue));
        }
    }

    private static byte[] ParseBase64WithoutPadding(string payload)
    {
        payload = payload.Trim().Replace('-', '+').Replace('_', '/');
        var base64 = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }
}
