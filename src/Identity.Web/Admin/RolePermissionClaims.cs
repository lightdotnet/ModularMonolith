using StarterKit.Modules.Identity.Models;
using StarterKit.Shared.Constants;

namespace StarterKit.Modules.Identity.Web.Admin;

/// <summary>
/// Builds a role's claim set after its permissions were edited in the admin UI.
/// </summary>
internal static class RolePermissionClaims
{
    /// <summary>
    /// Keeps every non-permission claim and every permission claim the UI does not know about
    /// (e.g. defined by a module that is not loaded in this host), and replaces the known
    /// permission claims with <paramref name="selected"/> (unknown selections are ignored).
    /// </summary>
    public static List<ClaimDto> Merge(
        IEnumerable<ClaimDto> existing,
        IEnumerable<string> selected,
        IEnumerable<string> known)
    {
        var knownSet = known.ToHashSet(StringComparer.Ordinal);

        var kept = existing
            .Where(c => c.Type != ClaimTypeConstants.Permission || !knownSet.Contains(c.Value));

        var granted = selected
            .Where(knownSet.Contains)
            .Distinct(StringComparer.Ordinal)
            .Select(p => new ClaimDto
            {
                Type = ClaimTypeConstants.Permission,
                Value = p,
            });

        return [.. kept, .. granted];
    }

    /// <summary>
    /// The permission values granted by <paramref name="claims"/>.
    /// </summary>
    public static HashSet<string> Permissions(IEnumerable<ClaimDto> claims) =>
        claims
            .Where(c => c.Type == ClaimTypeConstants.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
}
