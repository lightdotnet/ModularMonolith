using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Authentication;
using StarterKit.WebMvc.Infrastructure;
using StarterKit.WebMvc.Services.Identity;

namespace StarterKit.WebMvc.Pages.Account;

/// <summary>
/// Username/password sign-in — the port of the admin client's <c>loginAction</c>. The backend
/// decides password vs. Active Directory verification from the user record, so there is no
/// provider choice to send. POSTs are rate limited per client IP (<see cref="SignInRateLimiting"/>).
/// </summary>
[AllowAnonymous]
[EnableRateLimiting(SignInRateLimiting.PasswordPolicy)]
public sealed class LoginModel(
    IAuthClient authClient,
    ISessionSignInService sessionSignInService,
    IOptions<ExternalLoginOptions> externalLoginOptions)
    : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>Error handed over through TempData (external-login callback, sign-in rate limit).</summary>
    public string? Error { get; private set; }

    public bool ShowMicrosoftLogin => externalLoginOptions.Value.EnableMicrosoft;

    public IActionResult OnGet()
    {
        Error = SignInErrorMessage.Take(TempData);

        // Keep the login page unreachable once signed in, like the admin client's proxy.
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturnUrl.Sanitize(ReturnUrl));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await authClient.GetTokenAsync(
            new GetTokenRequest(
                Input.UserName,
                Input.Password),
            cancellationToken: cancellationToken);

        if (!result.IsSuccess || result.Data is null)
        {
            ModelState.AddModelError(
                string.Empty,
                string.IsNullOrWhiteSpace(result.Message) ? "Login failed." : result.Message);

            return Page();
        }

        await sessionSignInService.SignInAsync(
            HttpContext,
            result.Data,
            cancellationToken);

        return LocalRedirect(SafeReturnUrl.Sanitize(ReturnUrl));
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
