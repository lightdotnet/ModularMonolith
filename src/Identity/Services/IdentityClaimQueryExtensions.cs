using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Persistence;

namespace StarterKit.Modules.Identity.Services;

internal static class IdentityClaimQueryExtensions
{
    public static Task<bool> CheckUserHasClaimAsync(
        this IdentityDbContext context,
        string userId,
        string claimType,
        string claimValue)
    {
        return context.QueryUserClaims(userId)
            .AnyAsync(x => x.Type == claimType && x.Value == claimValue);
    }

    public static IQueryable<ClaimDto> QueryUserClaims(
        this IdentityDbContext context,
        string userId)
    {
        return context.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.ClaimType != null && c.ClaimValue != null)
            .Select(c => new ClaimDto
            {
                Type = c.ClaimType!,
                Value = c.ClaimValue!
            })

            .Union(
                from ur in context.UserRoles.AsNoTracking()
                where ur.UserId == userId
                join rc in context.RoleClaims.AsNoTracking()
                    on ur.RoleId equals rc.RoleId
                where rc.ClaimType != null && rc.ClaimValue != null
                select new ClaimDto
                {
                    Type = rc.ClaimType!,
                    Value = rc.ClaimValue!
                }
            );
    }

    /// <summary>
    /// Ids of the users holding the given claim, either directly (a user claim) or through one
    /// of their roles (a role claim). The user-to-claim counterpart of <see cref="QueryUserClaims"/>.
    /// </summary>
    public static IQueryable<string> QueryUserIdsWithClaim(
        this IdentityDbContext context,
        string claimType,
        string claimValue)
    {
        return context.UserClaims
            .AsNoTracking()
            .Where(c => c.ClaimType == claimType && c.ClaimValue == claimValue)
            .Select(c => c.UserId)

            .Union(
                from rc in context.RoleClaims.AsNoTracking()
                where rc.ClaimType == claimType && rc.ClaimValue == claimValue
                join ur in context.UserRoles.AsNoTracking()
                    on rc.RoleId equals ur.RoleId
                select ur.UserId
            );
    }
}
