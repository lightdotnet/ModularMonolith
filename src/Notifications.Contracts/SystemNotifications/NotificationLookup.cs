using StarterKit.Shared;

namespace StarterKit.Notifications.Contracts.SystemNotifications;

public record NotificationLookup : PageQuery
{
    public string? ToUserId { get; set; }

    public NotificationStatus? Status { get; set; }
}