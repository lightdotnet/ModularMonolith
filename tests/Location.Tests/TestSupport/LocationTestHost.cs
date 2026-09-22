using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Locations.Api.Data;
using StarterKit.Shared;

namespace Location.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="LocationDbContext"/>, mirroring
/// <c>Organization.Tests.TestSupport.OrganizationTestHost</c>, so handlers under test exercise real
/// EF Core behavior (FK constraints, IQueryable translation) instead of hand-mocked stand-ins.
/// </summary>
public sealed class LocationTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public LocationTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
    {
        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddDbContext<LocationDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<LocationDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public LocationDbContext Context { get; }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
