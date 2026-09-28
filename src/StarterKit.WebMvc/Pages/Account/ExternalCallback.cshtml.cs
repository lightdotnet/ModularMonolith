using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Services.Identity;

namespace StarterKit.WebMvc.Pages.Account;

/// <summary>
/// Callback target of the Microsoft external-login relay — the port of the admin client's
/// <c>/login/microsoft/callback</c>: on <c>error</c>, bounces back to the login page with a fixed
/// message (the raw <c>error</c>/<c>error_description</c> are only logged — they arrive in the
/// query string, so anyone could craft them); otherwise checks <c>state</c> against the PKCE
/// cookie, redeems the one-time <c>code</c> (+ the stashed <c>code_verifier</c>) via
/// <c>POST auth/token/external</c>, establishes the session exactly like the password login, and
/// redirects to the sanitized original return URL.
/// </summary>
[AllowAnonymous]
public sealed class ExternalCallbackModel(
    ExternalLoginPkce externalLoginPkce,
    IAuthClient authClient,
    ISessionSignInService sessionSignInService,
    ILogger<ExternalCallbackModel> logger)
    : PageModel
{
    private const string FailedMessage = "Microsoft sign-in failed. Please try again.";

    public async Task<IActionResult> OnGetAsync(
        string? code,
        string? state,
        string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        // Always consumed (and cleared), whatever the outcome.
        var pkce = externalLoginPkce.Take(HttpContext);

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogInformation(
                "Microsoft external login returned an error: {Error} ({ErrorDescription}).",
                error,
                errorDescription);

            return LoginError(string.Equals(error, "access_denied", StringComparison.Ordinal)
                ? "Microsoft sign-in was cancelled."
                : FailedMessage);
        }

        if (string.IsNullOrEmpty(code) || pkce is null)
        {
            return LoginError("Your sign-in attempt expired. Please try again.");
        }

        if (!string.Equals(state, pkce.State, StringComparison.Ordinal))
        {
            return LoginError("Your sign-in attempt could not be verified. Please try again.");
        }

        var result = await authClient.ExchangeExternalCodeAsync(
            new ExchangeAuthCodeRequest(
                code,
                pkce.Verifier),
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            // The backend's own message (e.g. an unlinked account) is safe to show: it travels in
            // TempData, not in the URL.
            return LoginError(string.IsNullOrWhiteSpace(result.Message)
                ? FailedMessage
                : result.Message);
        }

        await sessionSignInService.SignInAsync(
            HttpContext,
            result.Data,
            cancellationToken);

        return LocalRedirect(SafeReturnUrl.Sanitize(pkce.State));
    }

    private RedirectToPageResult LoginError(string message)
    {
        SignInErrorMessage.Set(
            TempData,
            message);

        return RedirectToPage("/Account/Login");
    }
}
