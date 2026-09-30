using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StarterKit.Modules.Identity.Domain;
using System.Security.Claims;

namespace StarterKit.Modules.Identity.Web.Authentication;

/// <summary>
/// A <see cref="SignInManager{TUser}"/> that also enforces the user's domain status: a user who is
/// not active (<see cref="User.Status"/>) or is soft-deleted (<see cref="User.Deleted"/>) cannot sign
/// in, and an existing cookie of such a user is rejected when its security stamp is re-validated.
/// This is the same rule the JWT and external-login paths apply.
/// </summary>
internal sealed class IdentitySignInManager(
    UserManager<User> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<User> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<User>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<User> confirmation)
    : SignInManager<User>(
        userManager,
        contextAccessor,
        claimsFactory,
        optionsAccessor,
        logger,
        schemes,
        confirmation)
{
    public override async Task<bool> CanSignInAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.Status.IsActive || user.Deleted is not null)
        {
            Logger.LogWarning(
                "Sign-in rejected: user {UserId} is inactive or deleted.",
                user.Id);
            return false;
        }

        return await base.CanSignInAsync(user);
    }

    public override async Task<User?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        var user = await base.ValidateSecurityStampAsync(principal);

        if (user is null)
            return null;

        // A valid stamp is not enough: a user locked or deleted since the cookie was issued must
        // not keep the session alive on re-validation.
        return await CanSignInAsync(user) ? user : null;
    }
}
