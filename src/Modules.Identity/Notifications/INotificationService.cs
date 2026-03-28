using Monolith.Notifications;

namespace Monolith.Identity.Notifications;

public interface INotificationService
{
    Task SaveAsync(string fromUserId, string? fromName, string toUserId, SystemMessage message);

    Task<PagedResult<NotificationDto>> GetAsync(NotificationLookup request);

    Task<NotificationDto?> GetByIdAsync(string userId, string id);

    Task<int> CountUnreadAsync(string userId);

    Task MarkAsReadAsync(string userId, string id);

    Task ReadAllAsync(string userId);
}
