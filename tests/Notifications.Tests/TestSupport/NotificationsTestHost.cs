using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarterKit.Modules.Identity.Contracts;
using StarterKit.Modules.Notifications.Contracts.SystemNotifications;
using StarterKit.Modules.Notifications.Domain;
using StarterKit.Modules.Notifications.Persistence;
using StarterKit.Modules.Notifications.SignalR;
using StarterKit.Shared;

namespace Notifications.Tests.TestSupport;

/// <summary>
/// Wires a Sqlite in-memory <see cref="NotificationDbContext"/>, mirroring the other modules'
/// test hosts. Sqlite rather than the InMemory provider because <see cref="AddAsync"/> seeds
/// non-default statuses with <c>ExecuteUpdateAsync</c>, which the InMemory provider does not support.
/// </summary>
/// <remarks>
/// The SignalR hub is a mock, exposed as <see cref="Hub"/>, so tests can verify what was pushed.
/// Sqlite stores <see cref="DateTimeOffset"/> as Unix seconds, so <see cref="AddAsync"/> advances
/// <see cref="DateTime"/> by one second after each save to keep <c>Created</c> ordering deterministic.
/// </remarks>
internal sealed class NotificationsTestHost : IDisposable
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

    public Mock<IHubService> Hub { get; } = new();

    public Mock<IIdentityModuleApi> IdentityApi { get; } = new();

    public NotificationDbContext Context { get; }

    /// <summary>
    /// Seeds one notification through the <see cref="Notification.Create"/> factory. A status other
    /// than <see cref="NotificationStatus.None"/> is written straight to the row, because the domain
    /// API has no transition to <see cref="NotificationStatus.Archived"/>. The change tracker is
    /// cleared afterwards so the handler under test loads fresh rows, as it would in a new request.
    /// </summary>
    public async Task<Notification> AddAsync(
        string toUserId,
        NotificationStatus status,
        string title = "Title",
        string? senderName = null)
    {
        var notification = Notification.Create(
            toUserId,
            title,
            null,
            null,
            "sender",
            senderName);

        Context.Notifications.Add(notification);
        await Context.SaveChangesAsync();

        if (status != NotificationStatus.None)
        {
            await Context.Notifications
                .Where(x => x.Id == notification.Id)
                .ExecuteUpdateAsync(u => u.SetProperty(p => p.Status, status));
        }

        Context.ChangeTracker.Clear();
        DateTime.UtcNow = DateTime.UtcNow.AddSeconds(1);

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
