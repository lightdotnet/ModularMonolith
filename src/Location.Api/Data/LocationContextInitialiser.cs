using Microsoft.Extensions.Logging;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Persistence.MigrationSupport;

namespace StarterKit.Locations.Api.Data;

public class LocationContextInitialiser(
    ILogger<LocationContextInitialiser> logger,
    LocationDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }

    public async Task TrySeedAsync()
    {
        logger.LogInformation("location_module seeding data...");

        try
        {
            if (await context.Database.CanConnectAsync())
            {
                await SeedAsync();
                logger.LogInformation("location_module seed data completed");
            }
            else
            {
                logger.LogError("location_module cannot connect to DB");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "location_module seeding data error: {mess}", ex.Message);
            throw;
        }
    }

    public async Task SeedAsync()
    {
        // Reproduces the pre-refactor hardcoded rules exactly: Store/Warehouse are roots that can
        // have children; Terminal must be parented under a Store and is always a leaf; Bin must be
        // parented under a Warehouse and is always a leaf.
        await GetOrCreateLocationTypeAsync("STORE", "Store", null, canHaveChildren: true);
        await GetOrCreateLocationTypeAsync("WAREHOUSE", "Warehouse", null, canHaveChildren: true);
        await GetOrCreateLocationTypeAsync("TERMINAL", "Terminal", "STORE", canHaveChildren: false);
        await GetOrCreateLocationTypeAsync("BIN", "Bin", "WAREHOUSE", canHaveChildren: false);
    }

    private async Task<LocationType> GetOrCreateLocationTypeAsync(
        string id, string name, string? allowedParentTypeId, bool canHaveChildren)
    {
        var existing = await context.LocationTypes.SingleOrDefaultAsync(x => x.Id == id);

        if (existing is not null)
            return existing;

        var locationType = LocationType.Create(id, name, allowedParentTypeId, canHaveChildren);

        await context.LocationTypes.AddAsync(locationType);
        await context.SaveChangesAsync();

        logger.LogInformation("Location type {id} added", id);

        return locationType;
    }
}
