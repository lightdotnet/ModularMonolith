using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Notifications.Api.Data;

public class NotificationContextInitialiser(
    ILogger<NotificationContextInitialiser> logger,
    NotificationDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
