using Light.AspNetCore.Authorization;

namespace StarterKit.Modules.Identity.Web.Admin;

/// <summary>
/// Permission definitions sharing a group (<see cref="PermissionDefinition.Parent"/>), for the
/// grouped checklists of the admin pages.
/// </summary>
public sealed record PermissionGroup(
    string Name,
    IReadOnlyList<PermissionDefinition> Permissions)
{
    public const string DefaultGroupName = "general";

    public static IReadOnlyList<PermissionGroup> From(IEnumerable<PermissionDefinition> definitions) =>
    [
        .. definitions
            .GroupBy(d => string.IsNullOrWhiteSpace(d.Parent) ? DefaultGroupName : d.Parent)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PermissionGroup(
                g.Key,
                [.. g.OrderBy(d => d.Name, StringComparer.Ordinal)]))
    ];
}
