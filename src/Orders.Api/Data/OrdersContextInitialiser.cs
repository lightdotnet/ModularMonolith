using Microsoft.Extensions.Logging;
using StarterKit.Orders.Api.Domain.OrderTypes;
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

    public async Task TrySeedAsync()
    {
        logger.LogInformation("orders_module seeding data...");

        try
        {
            if (await context.Database.CanConnectAsync())
            {
                await SeedAsync();
                logger.LogInformation("orders_module seed data completed");
            }
            else
            {
                logger.LogError("orders_module cannot connect to DB");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "orders_module seeding data error: {mess}", ex.Message);
            throw;
        }
    }

    public async Task SeedAsync()
    {
        await GetOrCreateOrderTypeAsync("SHIPPING", OrderTypeCategory.Fee, "Shipping");
        await GetOrCreateOrderTypeAsync("OTHER", OrderTypeCategory.Fee, "Other");

        await GetOrCreateOrderTypeAsync("CASH", OrderTypeCategory.Payment, "Cash");
        await GetOrCreateOrderTypeAsync("CARD", OrderTypeCategory.Payment, "Card");
        await GetOrCreateOrderTypeAsync("BANK_TRANSFER", OrderTypeCategory.Payment, "Bank transfer");
        await GetOrCreateOrderTypeAsync("OTHER", OrderTypeCategory.Payment, "Other");
    }

    private async Task<OrderType> GetOrCreateOrderTypeAsync(
        string id,
        OrderTypeCategory category,
        string name)
    {
        // Looked up by the composite (Id, Category) key, not just Id — "OTHER" legitimately exists
        // once per category.
        var existing = await context.OrderTypes
            .SingleOrDefaultAsync(x => x.Id == id && x.Category == category);

        if (existing is not null)
            return existing;

        var orderType = OrderType.Create(id, category, name);

        await context.OrderTypes.AddAsync(orderType);
        await context.SaveChangesAsync();

        logger.LogInformation("Order type {id} ({category}) added", id, category);

        return orderType;
    }
}
