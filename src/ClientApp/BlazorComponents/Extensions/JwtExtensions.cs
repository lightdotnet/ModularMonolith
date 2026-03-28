using System.Security.Claims;
using System.Text.Json;
using ClaimTypes = Monolith.Claims.ClaimTypes;

namespace Monolith.Blazor.Extensions;

public class JwtExtensions
{
    /// <summary>
    /// return Claim to use ClaimsIdentity
    /// </summary>
    public static List<Claim> ReadClaims(string jwt)
    {
        var claims = new List<Claim>();

        string payload = jwt.Split('.')[1];
        byte[] jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs is not null)
        {
            // get roles
            var roleKey = ClaimTypes.Role;

            keyValuePairs.TryGetValue(roleKey, out object? roles);
            if (roles is not null)
            {
                string? rolesString = roles.ToString();
                if (!string.IsNullOrEmpty(rolesString))
                {
                    if (rolesString.Trim().StartsWith('['))
                    {
                        string[]? parsedRoles = JsonSerializer.Deserialize<string[]>(rolesString);

                        if (parsedRoles is not null)
                        {
                            claims.AddRange(parsedRoles.Select(role => new Claim(roleKey, role)));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(roleKey, rolesString));
                    }
                }
                keyValuePairs.Remove(roleKey);
            }

            // get permissions
            var permissionsKey = ClaimTypes.Permission;

            keyValuePairs.TryGetValue(permissionsKey, out object? permissions);
            if (permissions is not null)
            {
                string? permissionsString = permissions.ToString();
                if (!string.IsNullOrEmpty(permissionsString))
                {
                    if (permissionsString.Trim().StartsWith('['))
                    {
                        string[]? parsedPermissions = JsonSerializer.Deserialize<string[]>(permissionsString);

                        if (parsedPermissions is not null)
                        {
                            claims.AddRange(parsedPermissions.Select(p => new Claim(permissionsKey, p)));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(permissionsKey, permissionsString));
                    }
                }
                keyValuePairs.Remove(permissionsKey);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty)));
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string payload)
    {
        payload = payload.Trim().Replace('-', '+').Replace('_', '/');
        var base64 = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return Convert.FromBase64String(base64);
    }
}
