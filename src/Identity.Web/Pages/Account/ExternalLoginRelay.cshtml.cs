using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Api.ExternalLogin;
using StarterKit.Identity.Contracts.ExternalLogin;
// Disambiguate from Microsoft.AspNetCore.Authentication.IAuthenticationService.
using IAuthenticationService = StarterKit.Identity.Api.Jwt.IAuthenticationService;

namespace StarterKit.Identity.Web.Pages.Account;

/// <summary>
/// Callback target of <see cref="ExternalLoginStartModel"/>'s challenge. Mirrors
/// <see cref="ExternalLoginModel.OnGetAsync"/>'s resolve sequence, but - unlike that interactive
/// page - never calls <c>SignInAsync</c> or sets an <c>Identity.Web</c> cookie: on success it mints
/// a token pair via <see cref="IAuthenticationService.IssueTokenForUserAsync"/>, stages it as a
/// one-time code via <see cref="IExternalLoginAuthCodeStore"/>, and redirects the browser back to
/// the client-supplied <c>redirectUri</c> with that code (OAuth-authorization-code-style).
/// </summary>
[EnableRateLimiting("external-login")]
public class ExternalLoginRelayModel(
    SignInManager<User> signInManager,
    IExternalLoginService externalLoginService,
    IAuthenticationService authenticationService,
    IExternalLoginAuthCodeStore authCodeStore,
    IOptions<ExternalLoginRelayOptions> relayOptions) : PageModel
{
    private readonly ExternalLoginRelayOptions _relay = relayOptions.Value;

    public async Task<IActionResult> OnGetAsync()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return RedirectToLoginWithError("Unable to read the sign-in response.");

        var (redirectUri, state, codeChallenge) = ReadRelayItems(info.AuthenticationProperties);
        if (redirectUri is null || codeChallenge is null)
            return RedirectToLoginWithError("Unable to read the sign-in response.");

        var descriptor = ExternalClaimsMapper.ToDescriptor(info);
        if (descriptor is null)
            return RedirectToClientWithError(redirectUri, state, MessageFor(ExternalLoginRejectionReason.MissingRequiredClaims));

        var outcome = await externalLoginService.ResolveAsync(descriptor);

        switch (outcome.Status)
        {
            case ExternalLoginStatus.Linked:
            case ExternalLoginStatus.Provisioned:
                var tokenResult = await authenticationService.IssueTokenForUserAsync(outcome.UserId!, device: null);
                if (!tokenResult.IsSuccess)
                    return RedirectToClientWithError(redirectUri, state, "Sign-in failed.");

                var code = await authCodeStore.IssueAsync(
                    tokenResult.Data!,
                    codeChallenge,
                    TimeSpan.FromSeconds(_relay.AuthCodeTtlSeconds));

                return Redirect(BuildClientUrl(
                    redirectUri,
                    "code", Uri.EscapeDataString(code),
                    "state", Uri.EscapeDataString(state ?? string.Empty)));

            case ExternalLoginStatus.Rejected:
            default:
                return RedirectToClientWithError(redirectUri, state, MessageFor(outcome.Reason!.Value));
        }
    }

    private IActionResult RedirectToClientWithError(string redirectUri, string? state, string reason) =>
        Redirect(BuildClientUrl(
            redirectUri,
            "error", "access_denied",
            "error_description", Uri.EscapeDataString(reason),
            "state", Uri.EscapeDataString(state ?? string.Empty)));

    // Fallback for the cases where the client's own redirectUri could not be recovered from the
    // round trip (missing/expired correlation state) - there is nowhere attacker-uncontrolled to
    // send the browser back to, so fail into the interactive login page instead.
    private IActionResult RedirectToLoginWithError(string error) =>
        RedirectToPage("/Account/Login", new { Error = error });

    private static string BuildClientUrl(string redirectUri, params string[] queryPairs)
    {
        var query = string.Join('&', queryPairs.Chunk(2).Select(pair => $"{pair[0]}={pair[1]}"));
        return $"{redirectUri}?{query}";
    }

    private static (string? RedirectUri, string? State, string? CodeChallenge) ReadRelayItems(
        AuthenticationProperties? properties)
    {
        if (properties is null)
            return (null, null, null);

        properties.Items.TryGetValue(ExternalLoginRelayItemKeys.RedirectUri, out var redirectUri);
        properties.Items.TryGetValue(ExternalLoginRelayItemKeys.State, out var state);
        properties.Items.TryGetValue(ExternalLoginRelayItemKeys.CodeChallenge, out var codeChallenge);

        return (redirectUri, state, codeChallenge);
    }

    // Kept in sync by hand with ExternalLoginModel.MessageFor - that file is the existing
    // interactive login page and is intentionally left untouched by this relay flow.
    private static string MessageFor(ExternalLoginRejectionReason reason) => reason switch
    {
        ExternalLoginRejectionReason.MissingRequiredClaims =>
            "Your Microsoft account did not return the information required to sign in.",
        ExternalLoginRejectionReason.TenantNotAllowed =>
            "Your Microsoft organization is not permitted to sign in to this application.",
        ExternalLoginRejectionReason.EmailDomainNotAllowed =>
            "Your email domain is not permitted to sign in to this application.",
        ExternalLoginRejectionReason.EmailAlreadyRegistered =>
            "An account with this email already exists. Contact an administrator to link your Microsoft sign-in.",
        ExternalLoginRejectionReason.UserInactive =>
            "Your account is not active. Contact an administrator.",
        ExternalLoginRejectionReason.ProvisioningFailed =>
            "Your account could not be created. Contact an administrator.",
        _ => "Sign-in failed.",
    };
}
