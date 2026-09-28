using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace StarterKit.WebMvc.Authentication;

/// <summary>
/// PKCE state for the Microsoft external-login relay — the port of the admin client's
/// <c>external-login-pkce.ts</c> + <c>/login/microsoft/start</c>. The <c>code_verifier</c> (and the
/// <c>state</c> sent with it) live in a short-lived, httpOnly, Data-Protection-encrypted cookie
/// scoped to the callback path, between <c>/Account/ExternalLogin</c> (sets it) and
/// <c>/Account/ExternalCallback</c> (reads + clears it). The verifier never leaves this server
/// except in the final code exchange.
/// </summary>
public sealed class ExternalLoginPkce(
    IDataProtectionProvider dataProtectionProvider,
    IHostEnvironment environment)
{
    public const string CookieName = "webmvc_microsoft_login_pkce";

    public const string CallbackPath = "/Account/ExternalCallback";

    private static readonly TimeSpan CookieLifetime = TimeSpan.FromMinutes(5);

    private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
        .CreateProtector("StarterKit.WebMvc.ExternalLoginPkce")
        .ToTimeLimitedDataProtector();

    /// <summary>A 32-byte base64url <c>code_verifier</c> and its S256 <c>code_challenge</c>.</summary>
    public static (string Verifier, string Challenge) CreatePair()
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        return (verifier, challenge);
    }

    public void Store(
        HttpContext httpContext,
        string verifier,
        string state)
    {
        var payload = JsonSerializer.Serialize(new PkcePayload(
            verifier,
            state));

        httpContext.Response.Cookies.Append(
            CookieName,
            _protector.Protect(
                payload,
                CookieLifetime),
            BuildCookieOptions(httpContext));
    }

    /// <summary>Reads and always clears the cookie; <c>null</c> when missing, expired or tampered with.</summary>
    public PkcePayload? Take(HttpContext httpContext)
    {
        var value = httpContext.Request.Cookies[CookieName];

        httpContext.Response.Cookies.Delete(
            CookieName,
            BuildCookieOptions(httpContext));

        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PkcePayload>(_protector.Unprotect(value));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null;
        }
    }

    private CookieOptions BuildCookieOptions(HttpContext httpContext)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            // Always Secure outside Development (TLS may terminate at a proxy); plain-http dev follows the request.
            Secure = !environment.IsDevelopment() || httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = $"{httpContext.Request.PathBase}{CallbackPath}",
            MaxAge = CookieLifetime,
            IsEssential = true,
        };
    }

    public sealed record PkcePayload(
        string Verifier,
        string State);
}
