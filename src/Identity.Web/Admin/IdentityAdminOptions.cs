namespace StarterKit.Modules.Identity.Web.Admin;

/// <summary>
/// Configuration-only toggles for the Identity admin pages, bound from
/// <see cref="SectionName"/>. A disabled page has no route (404) and its links are hidden.
/// </summary>
public sealed class IdentityAdminOptions
{
    public const string SectionName = "IdentityWeb:Admin";

    /// <summary>
    /// Master switch for every page under <c>/Admin</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public bool Users { get; set; } = true;

    public bool Roles { get; set; } = true;

    public bool Permissions { get; set; } = true;

    /// <summary>
    /// Whether the named <see cref="AdminFeatures"/> entry is enabled; unknown names are disabled.
    /// </summary>
    public bool IsEnabled(string feature) => Enabled && feature switch
    {
        AdminFeatures.Admin => Users || Roles || Permissions,
        AdminFeatures.Users => Users,
        AdminFeatures.Roles => Roles,
        AdminFeatures.Permissions => Permissions,
        _ => false,
    };
}
