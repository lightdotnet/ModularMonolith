using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared.Constants;
using System.Security.Claims;

namespace StarterKit.Modules.Identity.Authentication;

/// <summary>
/// Adds the platform claim types (<see cref="ClaimTypeConstants.UserId"/> and
/// <see cref="ClaimTypeConstants.UserName"/>) to the cookie principal built by ASP.NET Core
/// Identity, so the shared current-user, audit and super-user checks work for cookie-authenticated
/// Razor Pages the same way they do for a self-issued JWT. Role and role-claim (permission) claims
/// are still added by the base factory.
/// </summary>
internal sealed class IdentityClaimsPrincipalFactory(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<User, Role>(
        userManager,
        roleManager,
        options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user).ConfigureAwait(false);

        if (!identity.HasClaim(c => c.Type == ClaimTypeConstants.UserId))
            identity.AddClaim(new Claim(ClaimTypeConstants.UserId, user.Id));

        if (!string.IsNullOrEmpty(user.UserName)
            && !identity.HasClaim(c => c.Type == ClaimTypeConstants.UserName))
        {
            identity.AddClaim(new Claim(ClaimTypeConstants.UserName, user.UserName));
        }

        return identity;
    }
}
