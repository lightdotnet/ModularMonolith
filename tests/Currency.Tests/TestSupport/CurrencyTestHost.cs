using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Currencies.Api.Data;
using CurrencyEntity = StarterKit.Currencies.Api.Domain.Currencies.Currency;
using StarterKit.Currencies.Api.Domain.ExchangeRates;
using StarterKit.Shared;

namespace Currency.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="CurrencyDbContext"/>, mirroring the other modules' test hosts, so
/// handlers and services under test exercise real EF Core behavior (filtered/unique indexes, IQueryable
/// translation). Sqlite stores <c>DateTimeOffset</c> with one-second precision, so fixtures use whole seconds.
/// </summary>
public sealed class CurrencyTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public CurrencyTestHost(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        CurrentUser = new FakeCurrentUser();
        DateTime = new FakeDateTime { UtcNow = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero) };

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddDbContext<CurrencyDbContext>(options => options.UseSqlite(_connection).AddInterceptors(interceptors));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<CurrencyDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public CurrencyDbContext Context { get; }

    /// <summary>A second context over the same connection (nothing tracked).</summary>
    public CurrencyDbContext NewContext() => _provider.CreateScope().ServiceProvider.GetRequiredService<CurrencyDbContext>();

    /// <summary>Seeds the base currency (VND) plus USD (2 dp) and JPY (0 dp) and returns nothing; look them up by code.</summary>
    public async Task SeedCurrenciesAsync()
    {
        Context.Currencies.Add(CurrencyEntity.CreateBase("VND", "Vietnamese dong", null, 0));
        Context.Currencies.Add(CurrencyEntity.Create("USD", "US dollar", "$", 2));
        Context.Currencies.Add(CurrencyEntity.Create("JPY", "Japanese yen", null, 0));

        await Context.SaveChangesAsync();
    }

    public async Task AddRateAsync(
        string code,
        decimal rate,
        DateTimeOffset effectiveFrom)
    {
        var currency = await Context.Currencies.SingleAsync(x => x.Id == code);

        Context.ExchangeRates.Add(ExchangeRate.Record(currency, rate, effectiveFrom, "test-user", null, null));

        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
