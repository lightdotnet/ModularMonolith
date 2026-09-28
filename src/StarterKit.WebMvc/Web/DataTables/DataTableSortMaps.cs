using StarterKit.Identity.Contracts;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.WebMvc.Web.DataTables;

/// <summary>A resolved backend sort: the wire <c>sortBy</c> field and <c>sortDirection</c> (<c>asc</c>/<c>desc</c>).</summary>
public sealed record BackendSort(
    string SortBy,
    string SortDirection);

/// <summary>
/// The one place mapping a data table's sortable column keys (<c>&lt;dt-column key="…"
/// sortable="true"&gt;</c>) to the backend's whitelisted <c>sortBy</c> names. A key that is not
/// mapped — or maps to a name outside the Contracts allow-list — is ignored, so the backend's
/// default ordering (newest first) applies.
/// </summary>
public static class DataTableSortMaps
{
    /// <summary><c>_UsersTable</c> → <c>GET user/search</c> (<see cref="SearchUserSortFields"/>).</summary>
    public static readonly IReadOnlyDictionary<string, string> Users = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["user"] = SearchUserSortFields.UserName,
        ["name"] = SearchUserSortFields.FullName,
        ["email"] = SearchUserSortFields.Email,
        ["status"] = SearchUserSortFields.Status,
    };

    /// <summary><c>_InboxTable</c> / <c>_NotificationsTable</c> → both notification searches (<see cref="NotificationSortFields"/>).</summary>
    public static readonly IReadOnlyDictionary<string, string> Notifications = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["created"] = NotificationSortFields.Created,
        ["title"] = NotificationSortFields.Title,
        ["status"] = NotificationSortFields.Status,
        ["from"] = NotificationSortFields.FromName,
    };

    /// <summary>The backend sort for <paramref name="query"/>'s current sort, or <c>null</c> when unsorted/unmapped.</summary>
    public static BackendSort? Resolve(
        PagedQuery query,
        IReadOnlyDictionary<string, string> map,
        IReadOnlyList<string> allowedFields)
    {
        if (!query.IsSorted
            || !map.TryGetValue(query.Sort!, out var sortBy)
            || !allowedFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        return new BackendSort(
            sortBy,
            query.Direction == SortDirection.Desc ? "desc" : "asc");
    }

    public static BackendSort? ForUsers(PagedQuery query) =>
        Resolve(
            query,
            Users,
            SearchUserSortFields.All);

    public static BackendSort? ForNotifications(PagedQuery query) =>
        Resolve(
            query,
            Notifications,
            NotificationSortFields.All);
}
