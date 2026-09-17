using StarterKit.Identity.Api.Data;
using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.Services;

public static class IdentityClaimQueryExtensions
{
    public static Task<bool> CheckUserHasClaimAsync(this IdentityDbContext context, string userId, string claimType, string claimValue)
    {
        return context.QueryUserClaims(userId)
            .AnyAsync(x => x.Type == claimType && x.Value == claimValue);
    }

    public static IQueryable<ClaimDto> QueryUserClaims(this IdentityDbContext context, string userId)
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
}
