using Light.Extensions.Caching;
using StarterKit.Identity.Api.Jwt;
using StarterKit.Identity.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace StarterKit.Identity.Api.ExternalLogin;

internal sealed class ExternalLoginAuthCodeStore(
    ICacheService cacheService) : IExternalLoginAuthCodeStore
{
    private const string KeyPrefix = "ext-login-code:";

    public async Task<string> IssueAsync(TokenDto token, string codeChallenge, TimeSpan ttl)
    {
        // Same CSPRNG, byte length, and encoding as the refresh token - no weaker entropy for a
        // bearer artifact that (briefly) stands in for a full token pair.
        var code = JwtHelper.GenerateRefreshToken();

        await cacheService.SetAsync(
            KeyPrefix + code,
            new StoredAuthCode(token, codeChallenge),
            slidingExpiration: ttl);

        return code;
    }

    public async Task<ExternalLoginAuthCodeResult> ConsumeAsync(string code, string codeVerifier)
    {
        var key = KeyPrefix + code;

        var stored = await cacheService.GetAsync<StoredAuthCode>(key);
        if (stored is null)
            return Failed();

        if (!string.Equals(ComputeCodeChallenge(codeVerifier), stored.CodeChallenge, StringComparison.Ordinal))
            return Failed();

        // No atomic get-and-delete is exposed by this cache abstraction, so there is a narrow,
        // accepted theoretical race between GetAsync and RemoveAsync under concurrent double-exchange
        // of the exact same valid code - mitigated by the short TTL, PKCE binding, and the
        // refresh-token-grade entropy of the code, and only exploitable by someone who already
        // possesses a valid, unexpired, unconsumed code.
        await cacheService.RemoveAsync(key);

        return new ExternalLoginAuthCodeResult
        {
            Status = ExternalLoginAuthCodeStatus.Success,
            Token = stored.Token,
        };
    }

    private static ExternalLoginAuthCodeResult Failed() => new()
    {
        Status = ExternalLoginAuthCodeStatus.Failed,
    };

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));

        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record StoredAuthCode(TokenDto Token, string CodeChallenge);
}
