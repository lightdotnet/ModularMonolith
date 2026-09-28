using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Services.Identity;

namespace StarterKit.WebMvc.Pages.Account;

/// <summary>
/// Signs out (POST + antiforgery only). Before clearing the cookie it revokes the backend session
/// (<c>PUT user_profile/token/revoke</c> with the token's <c>jti</c>) so the refresh token dies with
/// it — best effort: a failed revoke is logged and never blocks the logout.
/// </summary>
[AllowAnonymous]
public sealed class LogoutModel(
    IUserProfileClient userProfileClient,
    ILogger<LogoutModel> logger)
    : PageModel
{
    public IActionResult OnGet() => LocalRedirect(SafeReturnUrl.Default);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await TryRevokeSessionAsync(cancellationToken);

        await HttpContext.SignOutAsync(SessionDefaults.AuthenticationScheme);

        return RedirectToPage("/Account/Login");
    }

    private async Task TryRevokeSessionAsync(CancellationToken cancellationToken)
    {
        var tokenId = User.FindFirstValue(SessionClaimTypes.TokenId);

        if (User.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(tokenId))
        {
            return;
        }

        try
        {
            var result = await userProfileClient.RevokeSessionAsync(
                tokenId,
                cancellationToken);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Revoking backend session {TokenId} on logout failed ({Code}: {Message}); signing out locally anyway.",
                    tokenId,
                    result.Code,
                    result.Message);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Includes ApiAuthorizationException (e.g. the access token already expired).
            logger.LogWarning(
                ex,
                "Revoking backend session {TokenId} on logout failed; signing out locally anyway.",
                tokenId);
        }
    }
}
