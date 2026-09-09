using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using StarterKit.Identity.Api.Entities;

namespace StarterKit.Identity.Web.Pages.Account;

public class LoginModel(
    SignInManager<User> signInManager,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration) : PageModel
{
    public bool MicrosoftLoginEnabled => configuration.IsMicrosoftLoginEnabled();

    // A syntactically valid ASP.NET Core Identity v3 password hash used only to spend
    // equivalent PBKDF2 time when the username is unknown, removing a timing oracle.
    private const string DummyPasswordHash =
        "AQAAAAEAAYagAAAAEAECAwQFBgcICQoLDA0ODxAABw4VHCMqMTg/Rk1UW2JpcHd+hYyTmqGor7a9xMvS2Q==";

    [BindProperty]
    [Required]
    public string? UserName { get; set; }

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Error { get; set; }

    public void OnGet()
    {
        ReturnUrl ??= Url.Content("~/");
    }

    public IActionResult OnPostMicrosoft()
    {
        if (!MicrosoftLoginEnabled)
            return NotFound();

        return StartMicrosoftChallenge();
    }

    public async Task<IActionResult> OnPostPasswordAsync()
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var user = await signInManager.UserManager.FindByNameAsync(UserName!);
        if (user is null)
        {
            // Spend comparable password-hashing time so a missing user is not
            // distinguishable from a wrong password by response timing.
            _ = passwordHasher.VerifyHashedPassword(new User(), DummyPasswordHash, Password!);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, Password!, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl);
    }

    public async Task<IActionResult> OnPostSwitchAccountAsync()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl ??= Url.Content("~/");
        return RedirectToPage("/Account/Login", new { ReturnUrl });
    }

    private IActionResult StartMicrosoftChallenge()
    {
        ReturnUrl ??= Url.Content("~/");
        var redirectUrl = Url.Page("/Account/ExternalLogin", values: new { ReturnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties("Microsoft", redirectUrl!);

        return new ChallengeResult("Microsoft", properties);
    }
}
