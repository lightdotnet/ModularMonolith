using Light.EntityFrameworkCore.Extensions;
using Light.Specification;
using Mapster;
using Monolith.Identity.Data;
using Monolith.Notifications;

namespace Monolith.Identity.Notifications;

internal class NotificationService(IdentityDbContext context) : INotificationService
{
    public Task<PagedResult<NotificationDto>> GetAsync(NotificationLookup request)
    {
        return context.Notifications
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(request.ToUserId), x => x.ToUserId == request.ToUserId)
            .WhereIf(request.OnlyUnread == true, x => x.ReadStatus == false)
            .OrderByDescending(o => o.Created)
            .ProjectToType<NotificationDto>()
            .ToPagedResultAsync(request);
    }

    public Task<NotificationDto?> GetByIdAsync(string userId, string id)
    {
        return context.Notifications
            .AsNoTracking()
            .Where(x => x.Id == id && x.ToUserId == userId)
            .ProjectToType<NotificationDto>()
            .SingleOrDefaultAsync();
    }

    public Task<int> CountUnreadAsync(string userId)
    {
        return context.Notifications
            .Where(x => x.ToUserId == userId && x.ReadStatus == false)
            .CountAsync();
    }

    public async Task SaveAsync(string fromUserId, string? fromName, string toUserId, SystemMessage message)
    {
        var entity = new Notification
        {
            FromUserId = fromUserId,
            FromName = fromName,
            ToUserId = toUserId,
            Title = message.Title,
            Message = message.Message,
            Url = message.Url
        };

        await context.Notifications.AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public Task MarkAsReadAsync(string userId, string id)
    {
        return context.Notifications
            .Where(x => x.Id == id && x.ToUserId == userId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ReadStatus, true));
    }

    public Task ReadAllAsync(string userId)
    {
        return context.Notifications
            .Where(x => x.ToUserId == userId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.ReadStatus, true));
    }
}
