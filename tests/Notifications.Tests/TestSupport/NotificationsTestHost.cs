using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Notifications.Api.Data;
using StarterKit.Notifications.Api.Entities;
using StarterKit.Notifications.Contracts.SystemNotifications;
using StarterKit.Shared;

namespace Notifications.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="NotificationDbContext"/>, mirroring the other modules'
/// test hosts. Sqlite rather than the InMemory provider because the service under test uses
/// <c>ExecuteUpdateAsync</c>, which the InMemory provider does not support.
/// </summary>
public sealed class NotificationsTestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public NotificationsTestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton<IDateTime>(DateTime);
        services.AddDbContext<NotificationDbContext>(options => options.UseSqlite(_connection));

        _provider = services.BuildServiceProvider();

        Context = _provider.GetRequiredService<NotificationDbContext>();
        Context.Database.EnsureCreated();
    }

    public FakeCurrentUser CurrentUser { get; } = new();

    public FakeDateTime DateTime { get; } = new();

    public NotificationDbContext Context { get; }

    public async Task<Notification> AddAsync(
        string toUserId,
        NotificationStatus status,
        string title = "Title")
    {
        var notification = new Notification
        {
            FromUserId = "sender",
            ToUserId = toUserId,
            Title = title,
            Status = status,
        };

        Context.Notifications.Add(notification);
        await Context.SaveChangesAsync();

        return notification;
    }

    /// <summary>Reads statuses straight from the database, bypassing the change tracker.</summary>
    public Task<Dictionary<string, NotificationStatus>> StatusesAsync() =>
        Context.Notifications
            .AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Status);

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
