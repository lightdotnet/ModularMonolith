using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using System.Security.Claims;

namespace StarterKit.Persistence.MigrationSupport;

/// <summary>
/// System identity used when running EF Core migrations outside an HTTP request, where no JWT/claims exist.
/// Represents itself via a synthetic <see cref="ClaimsPrincipal"/> carrying only the "Migrator" user id,
/// so it reuses <see cref="CurrentUserBase"/>'s claim-reading logic instead of duplicating it.
/// </summary>
internal class MigratorCurrentUser : CurrentUserBase
{
    public MigratorCurrentUser()
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypeConstants.UserId, "Migrator")
        ]));
    }
}
