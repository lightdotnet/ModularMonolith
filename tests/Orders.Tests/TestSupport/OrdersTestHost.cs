using Light.Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Orders.Api.Data;
using StarterKit.Shared;

namespace Orders.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="OrdersDbContext"/>, mirroring
/// <c>Approval.Tests.TestSupport.ApprovalTestHost</c>, so handlers under test exercise real EF Core
/// behavior (owned-type persistence, FK constraints, IQueryable translation) instead of
/// hand-mocked stand-ins.
/// </summary>
public sealed class OrdersTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public OrdersTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
    {
        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddSingleton<IPublisher>(Publisher);
        services.AddDbContext<OrdersDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<OrdersDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public RecordingPublisher Publisher { get; } = new();

    public OrdersDbContext Context { get; }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
