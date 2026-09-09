using Light.Mediator;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Approval.Api.Data;
using StarterKit.Shared;

namespace Approval.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="ApprovalDbContext"/>, mirroring
/// <c>Organization.Tests.TestSupport.OrganizationTestHost</c>, so handlers under test exercise
/// real EF Core behavior (IQueryable translation, cascade delete) instead of hand-mocked stand-ins.
/// </summary>
public sealed class ApprovalTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public ApprovalTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
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
        services.AddDbContext<ApprovalDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<ApprovalDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public RecordingPublisher Publisher { get; } = new();

    public ApprovalDbContext Context { get; }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
