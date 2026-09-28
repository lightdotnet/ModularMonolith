using StarterKit.Identity.Contracts.Authorization;
using StarterKit.Notifications.Contracts.Authorization;

namespace StarterKit.WebMvc.Web.Navigation;

public sealed record NavItem(
    string Title,
    string Icon,
    string Url,
    IReadOnlyList<string> Permissions)
{
    /// <summary>
    /// Path prefixes that mark this item active; defaults to <see cref="Url"/>. An item whose URL is
    /// a prefix of a sibling's (e.g. <c>/notifications</c> vs <c>/notifications/admin</c>) lists
    /// exact paths via <see cref="ExactMatch"/>.
    /// </summary>
    public bool ExactMatch { get; init; }

    /// <summary>Extra path prefixes (besides <see cref="Url"/>) that also mark this item active.</summary>
    public IReadOnlyList<string> ActivePrefixes { get; init; } = [];
}

public sealed record NavSection(
    string? Title,
    IReadOnlyList<NavItem> Items);

/// <summary>
/// The sidebar menu — the one place screens are registered. An item with permissions is shown when
/// the user holds any of them (or is a super admin); an item without permissions is shown to every
/// signed-in user.
/// </summary>
public static class NavigationMenu
{
    public static IReadOnlyList<NavSection> Sections { get; } =
    [
        new NavSection(
            null,
            [
                new NavItem(
                    "Dashboard",
                    "speedometer2",
                    "/",
                    [])
                {
                    ExactMatch = true,
                },
            ]),
        new NavSection(
            "Identity",
            [
                new NavItem(
                    "Users",
                    "people",
                    "/identity/users",
                    [IdentityPermissions.Users.View]),
                new NavItem(
                    "Roles",
                    "shield-lock",
                    "/identity/roles",
                    [IdentityPermissions.Roles.View]),
            ]),
        new NavSection(
            "Notifications",
            [
                new NavItem(
                    "Inbox",
                    "inbox",
                    "/notifications",
                    [])
                {
                    ExactMatch = true,
                    ActivePrefixes = ["/notifications/view"],
                },
                new NavItem(
                    "All notifications",
                    "bell",
                    "/notifications/admin",
                    [NotificationPermissions.Read])
                {
                    ExactMatch = true,
                },
                new NavItem(
                    "Send notification",
                    "send",
                    "/notifications/admin/send",
                    [NotificationPermissions.Send]),
            ]),
    ];
}
