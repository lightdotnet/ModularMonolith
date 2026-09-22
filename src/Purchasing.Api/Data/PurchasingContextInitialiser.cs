using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Purchasing.Api.Data;

public class PurchasingContextInitialiser(
    ILogger<PurchasingContextInitialiser> logger,
    PurchasingDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
