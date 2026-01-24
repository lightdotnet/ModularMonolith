using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monolith.Blazor.Extensions;
using Monolith.Blazor.Services;
using Monolith.HttpApi.Identity;
using System.ComponentModel.DataAnnotations;

namespace Monolith.Blazor.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty]
    [Required]
    public string UserName { get; set; } = default!;

    [BindProperty]
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [BindProperty]
    public bool RememberMe { get; set; } = true;

    public async Task<IActionResult> OnGet(string? returnUrl)
    {
        returnUrl = BuildReturnUrl(returnUrl);

        var tokenStorage = HttpContext.RequestServices.GetRequiredService<TokenStorage>();

        var savedToken = await tokenStorage.GetAsync();

        if (savedToken is not null && savedToken.IsNearlyExpired())
        {
            if (string.IsNullOrEmpty(savedToken.RefreshToken))
            {
                ModelState.AddModelError("", "Your session has expired, please login again.");

                return Page();
            }

            var tokenService = HttpContext.RequestServices.GetRequiredService<TokenHttpService>();

            var refreshToken = await tokenService.RefreshTokenAsync(savedToken.Token, savedToken.RefreshToken);

            if (refreshToken.Succeeded is false)
            {
                ModelState.AddModelError("", "Error when refresh your session.");

                return Page();
            }

            var tokenData = new TokenModel(refreshToken.Data.AccessToken, refreshToken.Data.ExpiresIn, refreshToken.Data.RefreshToken);

            await tokenStorage.SaveAsync(tokenData);

            var refreshSession = await HttpContext.SignInAsync(refreshToken.Data, true);

            if (refreshSession.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
        }

        if (HttpContext.User.Identity?.IsAuthenticated is true)
        {
            var userProfileService = HttpContext.RequestServices.GetRequiredService<UserProfileHttpService>();

            var getUserProfiles = await userProfileService.GetAsync();

            if (getUserProfiles.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPost(string? returnUrl)
    {
        var tokenService = HttpContext.RequestServices.GetRequiredService<TokenHttpService>();

        var getToken = await tokenService.GetTokenAsync(UserName, Password);

        if (getToken.Succeeded is false)
        {
            ModelState.AddModelError("", getToken.Message);

            return Page();
        }

        var tokenData = new TokenModel(getToken.Data.AccessToken, getToken.Data.ExpiresIn, getToken.Data.RefreshToken);

        // save token before getting user profile
        var tokenStorage = HttpContext.RequestServices.GetRequiredService<TokenStorage>();
        await tokenStorage.SaveAsync(tokenData);

        var login = await HttpContext.SignInAsync(getToken.Data, RememberMe);

        if (login.Succeeded is false)
        {
            ModelState.AddModelError("", login.Message);

            return Page();
        }

        returnUrl = BuildReturnUrl(returnUrl);

        return LocalRedirect(returnUrl);
    }

    private string BuildReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) ||
            returnUrl.Contains("/Error", StringComparison.OrdinalIgnoreCase) ||
            !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return returnUrl switch
        {
            null or "/" or "" => "~/",
            _ => $"~/{Url.Content(returnUrl)}".Replace("//", "/")
        };
    }
}
