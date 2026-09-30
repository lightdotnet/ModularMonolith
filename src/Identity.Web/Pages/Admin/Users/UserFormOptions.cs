using Microsoft.AspNetCore.Mvc.Rendering;
using StarterKit.Modules.Identity.Extensions;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Web.Pages.Admin.Users;

/// <summary>
/// Select options shared by the user create/edit forms.
/// </summary>
internal static class UserFormOptions
{
    /// <summary>
    /// Authentication providers in their wire form (empty for a local account).
    /// </summary>
    public static IEnumerable<SelectListItem> AuthProviders() =>
    [
        new("Local account", string.Empty),
        new("Active Directory", AuthProviderWire.ActiveDirectory),
        new("Microsoft (Entra ID)", AuthProviderWire.EntraId),
    ];

    /// <summary>
    /// Statuses an admin can set (the user aggregate only accepts Active and Locked), plus the
    /// user's <paramref name="current"/> status when it is another one, so the form does not
    /// silently switch it by pre-selecting the first option.
    /// </summary>
    public static IEnumerable<SelectListItem> Statuses(string? current)
    {
        string[] settable = [nameof(ActiveStatus.State.Active), nameof(ActiveStatus.State.Locked)];

        var statuses = string.IsNullOrEmpty(current) || settable.Contains(current)
            ? settable
            : [current, .. settable];

        return statuses.Select(s => new SelectListItem(s, s));
    }
}
