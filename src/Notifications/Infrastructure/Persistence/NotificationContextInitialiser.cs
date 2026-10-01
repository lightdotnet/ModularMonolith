using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Modules.Notifications.Infrastructure.Persistence;

internal class NotificationContextInitialiser(
    ILogger<NotificationContextInitialiser> logger,
    NotificationDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
