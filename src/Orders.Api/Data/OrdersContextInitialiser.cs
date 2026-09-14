using Microsoft.Extensions.Logging;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Orders.Api.Data;

public class OrdersContextInitialiser(
    ILogger<OrdersContextInitialiser> logger,
    OrdersDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }
}
