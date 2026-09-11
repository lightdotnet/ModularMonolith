using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.LeaveManagement.Api.Application.LeaveRequests;
using StarterKit.LeaveManagement.Api.Data;
using StarterKit.Shared;

namespace LeaveManagement.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="LeaveManagementDbContext"/>, mirroring
/// <c>Organization.Tests.TestSupport.OrganizationTestHost</c>, so handlers under test exercise
/// real EF Core behavior (IQueryable translation, indexes) instead of hand-mocked stand-ins.
/// </summary>
public sealed class LeaveManagementTestHost : IDisposable
{
    // Mapster's TypeAdapterConfig is process-global; outside the real host, nothing ever calls
    // LeaveManagementModule.Add, so without this the Period -> StartDate/EndDate flattening
    // configured in LeaveRequestMappingConfig would silently never run and Adapt<LeaveRequestDto>()
    // would map both dates to default. Registered once per test run, not per host instance.
    private static readonly bool MappingRegistered = RegisterMapping();

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public LeaveManagementTestHost(FakeCurrentUser? currentUser = null, FakeDateTime? dateTime = null)
    {
        _ = MappingRegistered;

        CurrentUser = currentUser ?? new FakeCurrentUser();
        DateTime = dateTime ?? new FakeDateTime();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddDbContext<LeaveManagementDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<LeaveManagementDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; }

    public FakeDateTime DateTime { get; }

    public LeaveManagementDbContext Context { get; }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private static bool RegisterMapping()
    {
        LeaveRequestMappingConfig.Register();
        return true;
    }
}
