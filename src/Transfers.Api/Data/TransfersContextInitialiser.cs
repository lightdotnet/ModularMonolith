using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Transfers.Api.Data;

public class TransfersContextInitialiser(
    ILogger<TransfersContextInitialiser> logger,
    TransfersDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
