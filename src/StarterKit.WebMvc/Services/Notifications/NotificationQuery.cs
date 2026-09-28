using System.Globalization;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.WebMvc.Services.Notifications;

internal static class NotificationQuery
{
    /// <summary>
    /// Maps a <see cref="NotificationLookup"/> onto the query string both notification endpoints bind
    /// with <c>[FromQuery]</c>. <paramref name="includeRecipient"/> is false for the caller-scoped
    /// <c>user_notification</c> endpoint, which must never receive a <c>toUserId</c>.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> From(
        NotificationLookup lookup,
        bool includeRecipient = true)
    {
        return new Dictionary<string, string?>
        {
            ["toUserId"] = includeRecipient ? lookup.ToUserId : null,
            ["status"] = lookup.Status?.ToString(),
            ["pageNumber"] = lookup.PageNumber.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = lookup.PageSize.ToString(CultureInfo.InvariantCulture),
            ["sortBy"] = lookup.SortBy,
            ["sortDirection"] = lookup.SortBy is null ? null : lookup.SortDirection,
        };
    }
}
