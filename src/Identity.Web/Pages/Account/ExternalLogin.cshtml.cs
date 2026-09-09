using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts.ExternalLogin;

namespace StarterKit.Identity.Web.Pages.Account;

public class ExternalLoginModel(
    SignInManager<User> signInManager,
    IExternalLoginService externalLoginService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        ReturnUrl ??= Url.Content("~/");

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
            return RedirectToLoginWithError("Unable to read the sign-in response.");

        var descriptor = ExternalClaimsMapper.ToDescriptor(info);
        if (descriptor is null)
            return RedirectToLoginWithError(MessageFor(ExternalLoginRejectionReason.MissingRequiredClaims));

        var outcome = await externalLoginService.ResolveAsync(descriptor);

        switch (outcome.Status)
        {
            case ExternalLoginStatus.Linked:
            case ExternalLoginStatus.Provisioned:
                var user = await signInManager.UserManager.FindByIdAsync(outcome.UserId!);
                if (user is null)
                    return RedirectToLoginWithError("Sign-in failed.");

                await signInManager.SignInAsync(user, isPersistent: false);
                await signInManager.UpdateExternalAuthenticationTokensAsync(info);
                return LocalRedirect(ReturnUrl);

            case ExternalLoginStatus.Rejected:
            default:
                return RedirectToLoginWithError(MessageFor(outcome.Reason!.Value));
        }
    }

    private IActionResult RedirectToLoginWithError(string error) =>
        RedirectToPage("/Account/Login", new { ReturnUrl, Error = error });

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
