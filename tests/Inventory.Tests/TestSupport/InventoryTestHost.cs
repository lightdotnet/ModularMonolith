using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Inventory.Api.Data;
using StarterKit.Shared;

namespace Inventory.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="InventoryDbContext"/>, mirroring
/// <c>Orders.Tests.TestSupport.OrdersTestHost</c>, so services/handlers under test exercise real EF
/// Core behavior (unique-index enforcement, FK constraints, IQueryable translation) instead of
/// hand-mocked stand-ins.
/// </summary>
public sealed class InventoryTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public InventoryTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
    {
        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddDbContext<InventoryDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<InventoryDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public InventoryDbContext Context { get; }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
