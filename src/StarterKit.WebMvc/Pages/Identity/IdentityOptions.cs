using Microsoft.AspNetCore.Mvc.Rendering;

namespace StarterKit.WebMvc.Pages.Identity;

/// <summary>Select options shared by the Identity user screens (same values as the admin client).</summary>
public static class IdentityOptions
{
    /// <summary>Backend wire values: empty = local account, <c>AD</c>, <c>Microsoft</c> (Entra ID).</summary>
    public static IEnumerable<SelectListItem> AuthProviders(string? selected)
    {
        var current = selected ?? string.Empty;

        return new[]
        {
            new SelectListItem("Local", string.Empty),
            new SelectListItem("Active Directory", "AD"),
            new SelectListItem("Microsoft", "Microsoft"),
        }
        .Select(item =>
        {
            item.Selected = string.Equals(item.Value, current, StringComparison.OrdinalIgnoreCase);
            return item;
        });
    }

    /// <summary>
    /// Status choices offered on edit (Active/Locked, as in the admin client); a user currently in
    /// any other status keeps that value as an extra option so saving does not silently change it.
    /// </summary>
    public static IEnumerable<SelectListItem> Statuses(string? selected)
    {
        var values = new List<string> { "Active", "Locked" };

        if (!string.IsNullOrEmpty(selected) && !values.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(selected);
        }

        return values.Select(value => new SelectListItem(
            value,
            value,
            string.Equals(value, selected, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Password is verified by the directory for AD/Microsoft accounts, so only local accounts need one.</summary>
    public static bool RequiresPassword(string? authProvider) => string.IsNullOrEmpty(authProvider);
}
