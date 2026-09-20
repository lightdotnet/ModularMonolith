using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Inventory.Api.Data;

public class InventoryContextInitialiser(
    ILogger<InventoryContextInitialiser> logger,
    InventoryDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
