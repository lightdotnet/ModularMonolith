namespace StarterKit.Modules.Identity.Web.Admin;

/// <summary>
/// Feature names of the admin pages, used by the <c>asp-feature</c> tag helper and the route
/// convention. <see cref="Admin"/> is the admin area as a whole.
/// </summary>
public static class AdminFeatures
{
    public const string Admin = "Admin";

    public const string Users = "Admin.Users";

    public const string Roles = "Admin.Roles";

    public const string Permissions = "Admin.Permissions";

    /// <summary>
    /// Root folder of the admin pages.
    /// </summary>
    public const string RootFolder = "/Admin";

    /// <summary>
    /// Maps a page's view-engine path (e.g. <c>/Admin/Users/Edit</c>) to its feature, or
    /// <c>null</c> when the page is not an admin page.
    /// </summary>
    public static string? FromPagePath(string? pagePath)
    {
        if (string.IsNullOrEmpty(pagePath)
            || !pagePath.StartsWith(RootFolder + "/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = pagePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // "/Admin/Index" (or any page directly in /Admin) belongs to the admin area as a whole.
        if (segments.Length < 3)
            return Admin;

        return segments[1].ToLowerInvariant() switch
        {
            "users" => Users,
            "roles" => Roles,
            "permissions" => Permissions,
            _ => Admin,
        };
    }
}
