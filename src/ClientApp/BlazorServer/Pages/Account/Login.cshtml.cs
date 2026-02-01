using Light.Contracts;
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
public class LoginModel(ILogger<LoginModel> logger) : PageModel
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

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

        if (savedToken is not null && savedToken.IsExpiringSoon())
        {
            if (string.IsNullOrEmpty(savedToken.RefreshToken))
            {
                ModelState.AddModelError("", "Your session has expired, please login again.");

                return Page();
            }

            Result refreshSessionResult = Result.Error();

            // We make sure the access token is only refreshed by one thread at a time. The other ones have to wait.
            await RefreshLock.WaitAsync();

            try
            {
                var tokenService = HttpContext.RequestServices.GetRequiredService<TokenHttpService>();

                var refreshToken = await tokenService.RefreshTokenAsync(savedToken.Token, savedToken.RefreshToken);

                if (refreshToken.Succeeded)
                {
                    var tokenData = new TokenModel(
                        refreshToken.Data.AccessToken,
                        refreshToken.Data.ExpiresIn,
                        refreshToken.Data.RefreshToken);

                    await tokenStorage.SaveAsync(tokenData);

                    refreshSessionResult = await HttpContext.SignInAsync(refreshToken.Data, true);
                }
            }
            finally
            {
                RefreshLock.Release();
            }

            if (refreshSessionResult.Succeeded)
            {
                logger.LogDebug("Token refreshed and user {username} signed in via refresh token.", HttpContext.User?.GetUserName());

                return LocalRedirect(returnUrl);
            }

            ModelState.AddModelError("", "Error when refresh your session.");

            return Page();
        }

        if (HttpContext.User.Identity?.IsAuthenticated is true)
        {
            var getUserProfiles = await HttpContext.GetUserProfilesAsync();

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
        returnUrl = returnUrl switch
        {
            null or "/" or "" => "~/",
            _ => $"~/{Url.Content(returnUrl)}".Replace("//", "/")
        };

        if (string.IsNullOrEmpty(returnUrl)
            || returnUrl.Contains("/Error", StringComparison.OrdinalIgnoreCase)
            || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "~/";
        }

        return returnUrl;
    }
}
