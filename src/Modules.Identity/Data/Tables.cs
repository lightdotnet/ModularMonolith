namespace Monolith.Identity.Data;

public abstract class Tables
{
    public const string Roles = nameof(Roles);

    public const string RoleClaims = nameof(RoleClaims);

    public const string Users = nameof(Users);

    public const string UserRoles = nameof(UserRoles);

    public const string UserClaims = nameof(UserClaims);

    public const string UserLogins = nameof(UserLogins);

    public const string UserTokens = nameof(UserTokens);

    /* Custom tables */

    public const string JwtTokens = nameof(JwtTokens);
}
