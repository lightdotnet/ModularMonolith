using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StarterKit.Identity.Api.Entities;

namespace StarterKit.Identity.Web.Pages.Account;

public class LogoutModel(SignInManager<User> signInManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        await signInManager.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl);

        return RedirectToPage("/Account/Login");
    }
}
