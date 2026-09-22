namespace StarterKit.Notifications.Contracts.SystemNotifications;

public class NotificationDto
{
    public string Id { get; set; } = null!;

    public string FromUserId { get; set; } = null!;

    public string? FromName { get; set; }

    public string ToUserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string? Url { get; set; }

    public NotificationStatus Status { get; set; }

    public DateTimeOffset Created { get; set; }
}