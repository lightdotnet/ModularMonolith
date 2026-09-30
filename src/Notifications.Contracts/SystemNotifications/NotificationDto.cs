namespace StarterKit.Modules.Notifications.Contracts.SystemNotifications;

public record NotificationDto
{
    public string Id { get; set; } = null!;

    public string ToUserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string? Url { get; set; }

    public string? SenderUserId { get; set; }

    public string? SenderName { get; set; }

    public NotificationStatus Status { get; set; }

    public DateTimeOffset Created { get; set; }
}
