using StarterKit.Modules.Notifications.Domain;

namespace StarterKit.Modules.Notifications.Application.Common;

/// <summary>
/// The notification store as the use cases see it, so the handlers do not depend on the
/// infrastructure context type.
/// </summary>
internal interface INotificationDbContext
{
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
