using System.Linq.Expressions;
using Light.EntityFrameworkCore.Extensions;
using Light.Specification;
using Mapster;
using Microsoft.EntityFrameworkCore;
using StarterKit.Notifications.Api.Data;
using StarterKit.Notifications.Api.Entities;
using StarterKit.Notifications.Api.SignalR;
using StarterKit.Notifications.Contracts.Services;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Notifications.Api.Services;

internal class NotificationService(
    NotificationDbContext context,
    IHubService hub) : INotificationService
{
    public Task<PagedResult<NotificationDto>> GetAsync(NotificationLookup request)
    {
        var query = context.Notifications
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(request.ToUserId), x => x.ToUserId == request.ToUserId)
            .WhereIf(request.Status.HasValue, x => x.Status == request.Status!.Value);

        return ApplySort(
                query,
                request.SortBy,
                request.SortDirection)
            .ProjectToType<NotificationDto>()
            .ToPagedResultAsync(request);
    }

    /// <summary>
    /// Applies the requested whitelisted sort, with the id as a stable tie-breaker for paging.
    /// An absent (or, defensively, unknown - this service is also callable in-process without
    /// the HTTP validator) sort field keeps the default ordering: newest first.
    /// </summary>
    private static IQueryable<Notification> ApplySort(
        IQueryable<Notification> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(
            sortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Notification>? ordered = sortBy?.Trim().ToLowerInvariant() switch
        {
            "created" => OrderBy(query, x => x.Created, descending),
            "title" => OrderBy(query, x => x.Title, descending),
            "status" => OrderBy(query, x => x.Status, descending),
            "fromname" => OrderBy(query, x => x.FromName, descending),
            _ => null,
        };

        if (ordered is null)
            return query.OrderByDescending(o => o.Created);

        return ordered.ThenBy(x => x.Id);
    }

    private static IOrderedQueryable<Notification> OrderBy<TKey>(
        IQueryable<Notification> query,
        Expression<Func<Notification, TKey>> keySelector,
        bool descending) =>
        descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);

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
            .Where(x => x.ToUserId == userId && x.Status == NotificationStatus.None)
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

    public async Task SendAsync(
        string fromUserId,
        string? fromName,
        string toUserId,
        SystemMessage message,
        CancellationToken cancellationToken = default)
    {
        await SaveAsync(fromUserId, fromName, toUserId, message);

        // Push after the record is saved so a client reacting to the push can load
        // the persisted entry from the API. The payload itself is sent to the client too.
        await hub.SendAsync(message, toUserId, cancellationToken);
    }

    public Task MarkAsReadAsync(string userId, string id)
    {
        return context.Notifications
            .Where(x => x.Id == id && x.ToUserId == userId)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.Status, NotificationStatus.Read));
    }

    public Task<int> ReadAllAsync(string userId)
    {
        // Only unread entries flip to Read - archived entries keep their status.
        return context.Notifications
            .Where(x => x.ToUserId == userId && x.Status == NotificationStatus.None)
            .ExecuteUpdateAsync(u => u.SetProperty(p => p.Status, NotificationStatus.Read));
    }
}
