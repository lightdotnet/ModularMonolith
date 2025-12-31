using Light.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Monolith.Blazor.Extensions;
using Monolith.Blazor.Services;
using Monolith.HttpApi.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGet(string? returnUrl)
    {
        returnUrl = returnUrl switch
        {
            "/" or null or "" => "~/",
            _ => $"~/{returnUrl}"
        };

        if (HttpContext.User.Identity?.IsAuthenticated is true)
        {
            return LocalRedirect(returnUrl);
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

        // save token in Server
        // var id = Guid.NewGuid().ToString("N");
        // tokenMemory.Save()

        var tokenData = new TokenModel(getToken.Data.AccessToken, getToken.Data.ExpiresIn, getToken.Data.RefreshToken);

        // save token before getting user profile
        var tokenStorage = HttpContext.RequestServices.GetRequiredService<TokenStorage>();
        await tokenStorage.SaveAsync(tokenData);

        var userClaims = JwtExtensions.ReadClaims(getToken.Data.AccessToken);

        var userProfileService = HttpContext.RequestServices.GetRequiredService<UserProfileHttpService>();

        var getUserProfiles = await userProfileService.GetAsync();

        if (getUserProfiles.Succeeded is false)
        {
            ModelState.AddModelError("", "Cannot get user profiles");

            return Page();
        }
        else
        {
            userClaims.AddRange(getUserProfiles.Data.BuildClaims());
        }

        var claimsIdentity = new ClaimsIdentity(userClaims, Constants.JwtAuthScheme);

        // Replace with new ClaimsPrincipal
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign in using Identity's scheme
        await HttpContext.SignInAsync(
            Constants.JwtAuthScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = RememberMe,  // "Remember me"
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(getToken.Data.ExpiresIn),
                AllowRefresh = true
            });

        returnUrl = returnUrl switch
        {
            "/" or null or "" => "/",
            _ => $"/{returnUrl}"
        };

        return LocalRedirect(returnUrl);
    }
}
