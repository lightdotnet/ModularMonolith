using Microsoft.AspNetCore.Identity;
using StarterKit.Identity.Api.Data;
using StarterKit.Identity.Api.Entities;
using StarterKit.Shared.Constants;
using System.Security.Claims;

namespace StarterKit.Identity.Api.Jwt;

internal class JwtTokenIssuer(
    UserManager<User> userManager,
    IdentityDbContext context,
    JwtSigningService jwtSigningService)
{
    public virtual async Task<string> IssueAsync(
        User user, string tokenId,
        DateTime tokenExpiresAt)
    {
        var claims = await GetUserClaimsAsync(user);

        claims.Add(new Claim(ClaimTypeConstants.TokenId, tokenId));

        return jwtSigningService.Generate(claims, tokenExpiresAt);
    }

    private async Task<IList<Claim>> GetUserClaimsAsync(User user)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        var roleIds = await context.Roles
            .Where(r => r.Name != null && userRoles.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync();

        var permissionClaims = await context.RoleClaims
            .Where(rc => roleIds.Contains(rc.RoleId))
            .Select(rc => new Claim(rc.ClaimType!, rc.ClaimValue!))
            .ToListAsync();

        // Per-user claims (e.g. employee_id, set by other modules via IUserService.SetClaimAsync)
        // are not read from RoleClaims above, so they must be merged in explicitly here.
        var userClaims = await userManager.GetClaimsAsync(user);

        var claims = new List<Claim>
        {
            { ClaimTypeConstants.UserId, user.Id },
            { ClaimTypeConstants.UserName, user.UserName },
        }
        .Union(permissionClaims)
        .Union(userClaims)
        .Where(x => !string.IsNullOrEmpty(x.Value))
        .ToList();

        return claims;
    }
}
