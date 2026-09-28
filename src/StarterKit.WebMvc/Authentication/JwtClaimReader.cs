using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// Decodes a JWT payload into claims without verifying its signature — the port of the admin
/// client's <c>lib/server/jwt.ts</c>. Safe here because the token was just issued to this server
/// by the backend (login/refresh response); the backend validates it on every API call. Only
/// string and string-array values are read (numeric claims such as <c>exp</c>/<c>iat</c> are
/// skipped, as in the admin client).
/// </summary>
internal static class JwtClaimReader
{
    /// <summary>Every string claim carried by the token, flattened; never throws — a malformed token yields none.</summary>
    public static IReadOnlyList<Claim> Read(string accessToken)
    {
        try
        {
            var segments = accessToken.Split('.');

            if (segments.Length < 2)
            {
                return [];
            }

            var payload = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(segments[1]));

            using var document = JsonDocument.Parse(payload);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var claims = new List<Claim>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                foreach (var value in ReadValues(property.Value))
                {
                    claims.Add(new Claim(
                        property.Name,
                        value));
                }
            }

            return claims;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return [];
        }
    }

    /// <summary>The backend serializes a single-valued claim as a string and a multi-valued one as an array.</summary>
    private static IEnumerable<string> ReadValues(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            yield return element.GetString()!;
            yield break;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in element.EnumerateArray())
        {
            yield return item.ValueKind == JsonValueKind.String
                ? item.GetString()!
                : item.GetRawText();
        }
    }
}
