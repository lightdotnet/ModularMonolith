using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Catalog.Api.Data;

public class CatalogContextInitialiser(
    ILogger<CatalogContextInitialiser> logger,
    CatalogDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
