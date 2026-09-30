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

    public string RecipientUserId { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string? Message { get; private set; }

    public string? Url { get; private set; }

    public string? SenderUserId { get; private set; }

    public string? SenderName { get; private set; }

    public NotificationStatus Status { get; private set; }

    public static Notification Create(
        string toUserId,
        string title,
        string? message,
        string? url,
        string? senderUserId,
        string? senderName) =>
        new()
        {
            RecipientUserId = toUserId,
            Title = title,
            Message = message,
            Url = url,
            Status = NotificationStatus.None,
            SenderUserId = senderUserId,
            SenderName = senderName,
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
