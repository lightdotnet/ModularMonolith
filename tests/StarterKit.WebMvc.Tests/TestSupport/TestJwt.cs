using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace StarterKit.WebMvc.Tests.TestSupport;

/// <summary>
/// Builds an unsigned JWT-shaped string (header.payload.signature) from a payload object — the
/// code under test only decodes the payload and never verifies the signature.
/// </summary>
public static class TestJwt
{
    public static string Create(object payload)
    {
        return string.Join(
            '.',
            Encode("""{"alg":"none","typ":"JWT"}"""),
            Encode(JsonSerializer.Serialize(payload)),
            "signature");
    }

    private static string Encode(string json) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));
}
