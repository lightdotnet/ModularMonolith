using Microsoft.AspNetCore.SignalR;

namespace StarterKit.Modules.Notifications.SignalR;

/// <summary>
/// <see cref="IHubService"/> over <see cref="IHubContext{THub}"/>. A typed payload is sent under
/// its type name as the client method name.
/// </summary>
internal sealed class HubService(IHubContext<SignalRHub> hubContext)
    : IHubService
{
    public Task NotifyAsync(CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync(
            NotificationConstants.ServerNotification,
            cancellationToken);

    public Task NotifyAsync(string userId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(userId).SendAsync(
            NotificationConstants.ServerNotification,
            cancellationToken);

    public Task NotifyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Users(userIds).SendAsync(
            NotificationConstants.ServerNotification,
            cancellationToken);

    public Task SendAsync<T>(T data, CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync(
            typeof(T).Name,
            data,
            cancellationToken);

    public Task SendAsync<T>(T data, string userId, CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(userId).SendAsync(
            typeof(T).Name,
            data,
            cancellationToken);

    public Task SendAsync<T>(T data, IEnumerable<string> userIds, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Users(userIds).SendAsync(
            typeof(T).Name,
            data,
            cancellationToken);
}
