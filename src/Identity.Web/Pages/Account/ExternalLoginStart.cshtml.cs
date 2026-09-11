using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using StarterKit.Identity.Api.Entities;

namespace StarterKit.Identity.Web.Pages.Account;

/// <summary>
/// Entry point of the authorization-code relay for a separate-origin client (e.g. the Next.js
/// admin app): validates the caller-supplied <c>provider</c> and <c>redirectUri</c>, then starts
/// the same Entra ID challenge <see cref="LoginModel.OnPostMicrosoft"/> uses for the interactive
/// login, but points the callback at <see cref="ExternalLoginRelayModel"/> instead of
/// <see cref="ExternalLoginModel"/>. Never establishes an <c>Identity.Web</c> cookie session itself.
/// </summary>
[EnableRateLimiting("external-login")]
public class ExternalLoginStartModel(
    SignInManager<User> signInManager,
    IConfiguration configuration,
    IOptions<ExternalLoginRelayOptions> relayOptions) : PageModel
{
    private readonly ExternalLoginRelayOptions _relay = relayOptions.Value;

    public IActionResult OnGet(string provider, string redirectUri, string state, string codeChallenge)
    {
        if (!configuration.IsMicrosoftLoginEnabled() || !IsSupportedProvider(provider))
            return NotFound();

        if (string.IsNullOrEmpty(redirectUri)
            || !_relay.AllowedRedirectUris.Contains(redirectUri, StringComparer.Ordinal))
        {
            return BadRequest();
        }

        var callbackUrl = Url.Page("/Account/ExternalLoginRelay");
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Microsoft", callbackUrl!);

        properties.Items[ExternalLoginRelayItemKeys.RedirectUri] = redirectUri;
        properties.Items[ExternalLoginRelayItemKeys.State] = state;
        properties.Items[ExternalLoginRelayItemKeys.CodeChallenge] = codeChallenge;

        return new ChallengeResult("Microsoft", properties);
    }

    // Only Microsoft (Entra ID) is wired up today - matches the single registered "Microsoft"
    // auth scheme in DependencyInjection.AddMicrosoftOpenIdConnect. "EntraId" is accepted as an
    // alias since that is the provider's current product name.
    private static bool IsSupportedProvider(string provider) =>
        string.Equals(provider, "Microsoft", StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider, "EntraId", StringComparison.OrdinalIgnoreCase);
}
