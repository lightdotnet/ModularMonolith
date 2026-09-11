using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.ExternalLogin;

/// <summary>
/// One-time, PKCE-bound relay for a <see cref="TokenDto"/> minted by the Entra ID external-login
/// flow in <c>Identity.Web</c>: the token is staged here under a short-lived, single-use code, and
/// handed to the browser only as that code in a redirect query string. The Next.js client's server
/// exchanges the code for the real token payload via <c>POST auth/token/external</c>. Pure
/// infrastructure, not a domain aggregate - analogous to <see cref="Jwt.HubTokenIssuer"/>.
/// </summary>
public interface IExternalLoginAuthCodeStore
{
    Task<string> IssueAsync(TokenDto token, string codeChallenge, TimeSpan ttl);

    Task<ExternalLoginAuthCodeResult> ConsumeAsync(string code, string codeVerifier);
}
