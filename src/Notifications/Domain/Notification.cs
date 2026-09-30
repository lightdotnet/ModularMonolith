using StarterKit.Shared.Entities;

namespace StarterKit.Modules.Notifications.Domain;

/// <summary>
/// A notification delivered to one user. Created through <see cref="Create"/>; its only
/// state transition is being marked as read.
/// </summary>
public class Notification : AuditableEntity
{
    // For EF Core materialization.
    private Notification()
    {
    }

    public string? FromUserId { get; private set; }

    public string? FromName { get; private set; }

    public string ToUserId { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string? Message { get; private set; }

    public string? Url { get; private set; }

    public NotificationStatus Status { get; private set; }

    public static Notification Create(
        string? fromUserId,
        string? fromName,
        string toUserId,
        string title,
        string? message,
        string? url) =>
        new()
        {
            FromUserId = fromUserId,
            FromName = fromName,
            ToUserId = toUserId,
            Title = title,
            Message = message,
            Url = url,
            Status = NotificationStatus.None,
        };

    /// <summary>
    /// Marks an unread notification as read. Idempotent, and an archived notification stays archived.
    /// </summary>
    public void MarkAsRead()
    {
        if (Status == NotificationStatus.None)
            Status = NotificationStatus.Read;
    }
}
