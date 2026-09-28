using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Infrastructure;

namespace StarterKit.WebMvc.Pages.Account;

/// <summary>
/// Starts the Microsoft external-login handshake — the port of the admin client's
/// <c>/login/microsoft/start</c>: generates a PKCE pair, stashes the verifier in a short-lived
/// protected cookie, and redirects the browser (top level) to Identity.Web's
/// <c>/Account/ExternalLoginStart</c> relay, which challenges Microsoft OIDC and redirects back
/// to <c>/Account/ExternalCallback</c> on success or failure.
/// </summary>
[AllowAnonymous]
[EnableRateLimiting(SignInRateLimiting.ExternalPolicy)]
public sealed class ExternalLoginModel(
    ExternalLoginPkce externalLoginPkce,
    IOptions<IdentityWebOptions> identityWebOptions,
    IOptions<ExternalLoginOptions> externalLoginOptions)
    : PageModel
{
    public IActionResult OnGet(string? returnUrl)
    {
        if (!externalLoginOptions.Value.EnableMicrosoft)
        {
            return NotFound();
        }

        var state = SafeReturnUrl.Sanitize(returnUrl);
        var (verifier, challenge) = ExternalLoginPkce.CreatePair();

        externalLoginPkce.Store(
            HttpContext,
            verifier,
            state);

        // Must match an entry of the backend's ExternalLoginRelay:AllowedRedirectUris exactly.
        var redirectUri = Url.Page(
            "/Account/ExternalCallback",
            pageHandler: null,
            values: null,
            protocol: Request.Scheme)!;

        var startUrl = new Uri(
            new Uri(identityWebOptions.Value.BaseUrl),
            "/Account/ExternalLoginStart");

        var target = QueryHelpers.AddQueryString(
            startUrl.ToString(),
            new Dictionary<string, string?>
            {
                ["provider"] = "Microsoft",
                ["redirectUri"] = redirectUri,
                ["state"] = state,
                ["codeChallenge"] = challenge,
            });

        return Redirect(target);
    }
}
