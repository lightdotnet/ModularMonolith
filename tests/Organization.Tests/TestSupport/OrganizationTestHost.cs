using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Organization.Api.Data;
using StarterKit.Shared;

namespace Organization.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="OrganizationDbContext"/>, mirroring
/// <c>Identity.Tests.TestSupport.IdentityTestHost</c>, so handlers under test exercise real
/// EF Core behavior (FK constraints, IQueryable translation) instead of hand-mocked stand-ins.
/// </summary>
public sealed class OrganizationTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public OrganizationTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
    {
        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddDbContext<OrganizationDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<OrganizationDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public OrganizationDbContext Context { get; }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
