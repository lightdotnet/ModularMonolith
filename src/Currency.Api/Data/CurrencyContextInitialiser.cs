using Microsoft.Extensions.Logging;
using StarterKit.Currencies.Api.Domain.Currencies;
using StarterKit.Persistence.Extensions;
using StarterKit.Persistence.MigrationSupport;
using StarterKit.Shared.Constants;

namespace StarterKit.Currencies.Api.Data;

public class CurrencyContextInitialiser(
    ILogger<CurrencyContextInitialiser> logger,
    CurrencyDbContext context)
{
    public virtual async Task InitialiseAsync()
    {
        await context.MigrateDatabaseAsync(logger);
    }

    public async Task TrySeedAsync()
    {
        logger.LogInformation("currency_module seeding data...");

        try
        {
            if (await context.Database.CanConnectAsync())
            {
                await SeedAsync();
                logger.LogInformation("currency_module seed data completed");
            }
            else
            {
                logger.LogError("currency_module cannot connect to DB");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "currency_module seeding data error: {mess}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Idempotent: seeds the default base currency (VND, no minor units) only when no base currency exists
    /// yet. An existing base — whichever currency it is — is never touched. A non-base VND with no base
    /// configured is an inconsistent state and throws.
    /// </summary>
    public async Task SeedAsync()
    {
        if (await context.Currencies.AnyAsync(x => x.IsBase))
            return;

        // The seed code may already exist as a regular currency; promoting it to base is out of scope, so
        // fail loudly and leave the fix to an operator rather than running with no base currency.
        if (await context.Currencies.AnyAsync(x => x.Id == CurrencyConstants.Default))
        {
            throw new InvalidOperationException(
                $"Currency '{CurrencyConstants.Default}' exists but is not the base currency, and no base currency is configured. "
                + "Fix the data manually (set exactly one currency as base) before seeding.");
        }

        var baseCurrency = Currency.CreateBase(
            CurrencyConstants.Default,
            "Vietnamese dong",
            symbol: null,
            decimalPlaces: 0);

        await context.Currencies.AddAsync(baseCurrency);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A parallel seeder run inserted the base first; that is fine as long as a base now exists.
            context.ChangeTracker.Clear();

            if (await context.Currencies.AnyAsync(x => x.IsBase))
            {
                logger.LogInformation("Base currency was seeded concurrently; nothing to do");
                return;
            }

            throw;
        }

        logger.LogInformation("Base currency {code} added", baseCurrency.Code);
    }
}
