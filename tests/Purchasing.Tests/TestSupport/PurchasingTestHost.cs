using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Light.Mediator;
using StarterKit.Shared;
using StarterKit.Purchasing.Api.Data;

namespace Purchasing.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="PurchasingDbContext"/>, mirroring
/// <c>Orders.Tests.TestSupport.OrdersTestHost</c>, so handlers under test exercise real EF Core
/// behavior (unique-index enforcement, concurrency tokens, IQueryable translation) instead of
/// hand-mocked stand-ins. Sqlite stores <c>DateTimeOffset</c> with one-second precision, so fixtures
/// use whole minutes.
/// </summary>
public sealed class PurchasingTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public PurchasingTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
    {
        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime { UtcNow = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero) };

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddSingleton<IPublisher>(Publisher);
        services.AddDbContext<PurchasingDbContext>(options => options.UseSqlite(_connection).AddInterceptors(SaveFaults));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<PurchasingDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public RecordingPublisher Publisher { get; } = new();

    public SaveFaultInterceptor SaveFaults { get; } = new();

    public PurchasingDbContext Context { get; }

    /// <summary>A second context over the same connection, e.g. to simulate a concurrent writer.</summary>
    public PurchasingDbContext NewContext() => _provider.CreateScope().ServiceProvider.GetRequiredService<PurchasingDbContext>();

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
