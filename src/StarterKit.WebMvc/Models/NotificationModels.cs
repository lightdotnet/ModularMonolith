using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.WebMvc.Web.DataTables;

namespace StarterKit.WebMvc.Models;

public sealed record MyNotificationsIndexViewModel(
    DataTableResult<NotificationDto> Table,
    NotificationStatus? Status);

public sealed record NotificationsAdminIndexViewModel(
    DataTableResult<NotificationDto> Table,
    NotificationStatus? Status,
    string? ToUserId);

/// <summary>Send form: the recipient plus the Contracts <see cref="SystemMessage"/> being sent.</summary>
public sealed class SendNotificationInput
{
    [Required(ErrorMessage = "Recipient is required.")]
    [Display(Name = "Recipient user ID")]
    public string ToUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "The notification content is required.")]
    public SystemMessage? Message { get; set; } = new();
}

public sealed record UserLookupItem(
    string Id,
    string UserName,
    string DisplayName);

/// <summary>Display helpers for <see cref="NotificationStatus"/>.</summary>
public static class NotificationDisplay
{
    public static string Text(NotificationStatus status) => status switch
    {
        NotificationStatus.None => "Unread",
        NotificationStatus.Read => "Read",
        NotificationStatus.Archived => "Archived",
        _ => status.ToString(),
    };

    public static string Variant(NotificationStatus status) => status switch
    {
        NotificationStatus.None => "primary",
        NotificationStatus.Archived => "dark",
        _ => "secondary",
    };

    public static IEnumerable<SelectListItem> StatusOptions(NotificationStatus? selected) =>
        Enum.GetValues<NotificationStatus>()
            .Select(status => new SelectListItem(
                Text(status),
                status.ToString(),
                status == selected));

    /// <summary>Only http(s) and site-relative URLs are rendered as links (never <c>javascript:</c> etc.).</summary>
    public static bool IsSafeLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
