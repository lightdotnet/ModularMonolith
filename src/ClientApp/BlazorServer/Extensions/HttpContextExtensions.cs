using Light.Contracts;
using Light.Identity;
using Microsoft.AspNetCore.Authentication;
using Monolith.Blazor.Services;
using Monolith.HttpApi.Identity;
using System.Security.Claims;

namespace Monolith.Blazor.Extensions;

public static class HttpContextExtensions
{
    public static async Task<Result> SignInAsync(this HttpContext httpContext, TokenDto token, bool rememberMe)
    {
        var userClaims = JwtExtensions.ReadClaims(token.AccessToken);

        var getUserProfiles = await httpContext.GetUserProfilesAsync();

        if (getUserProfiles.Succeeded is false)
        {
            return Result.Error("Cannot get user profiles");
        }
        else
        {
            userClaims.AddRange(getUserProfiles.Data.BuildClaims());
        }

        var claimsIdentity = new ClaimsIdentity(userClaims, Constants.JwtAuthScheme);

        // Replace with new ClaimsPrincipal
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign in using Identity's scheme
        await httpContext.SignInAsync(
            Constants.JwtAuthScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,  // "Remember me"
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
                AllowRefresh = true
            });

        return Result.Success();
    }

    public static Task<Result<UserDto>> GetUserProfilesAsync(this HttpContext httpContext)
    {
        var userProfileService = httpContext.RequestServices.GetRequiredService<UserProfileHttpService>();

        return userProfileService.GetAsync();
    }
}
