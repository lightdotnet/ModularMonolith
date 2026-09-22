using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Light.Mediator;
using StarterKit.Shared;
using StarterKit.Transfers.Api.Data;

namespace Transfers.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="TransfersDbContext"/>, mirroring
/// <c>Orders.Tests.TestSupport.OrdersTestHost</c>, so handlers under test exercise real EF Core
/// behavior (unique-index enforcement, concurrency tokens, IQueryable translation) instead of
/// hand-mocked stand-ins. Sqlite stores <c>DateTimeOffset</c> with one-second precision, so fixtures
/// use whole minutes.
/// </summary>
public sealed class TransfersTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public TransfersTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
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
        services.AddDbContext<TransfersDbContext>(options => options.UseSqlite(_connection).AddInterceptors(SaveFaults));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<TransfersDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public RecordingPublisher Publisher { get; } = new();

    public SaveFaultInterceptor SaveFaults { get; } = new();

    public TransfersDbContext Context { get; }

    /// <summary>A second context over the same connection, e.g. to simulate a concurrent writer.</summary>
    public TransfersDbContext NewContext() => _provider.CreateScope().ServiceProvider.GetRequiredService<TransfersDbContext>();

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
