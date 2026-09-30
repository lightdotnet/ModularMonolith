using StarterKit.Shared;

namespace StarterKit.Modules.Notifications.Contracts.SystemNotifications;

public record NotificationLookup : PageQuery
{
    public string? ToUserId { get; set; }

    public NotificationStatus? Status { get; set; }
}
