namespace StarterKit.Modules.Notifications.Application.Common;

/// <summary>
/// Pushes messages to connected clients over the SignalR notification hub.
/// </summary>
internal interface IHubService
{
    Task NotifyAsync(CancellationToken cancellationToken = default);

    Task NotifyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    Task NotifyAsync(string userId, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T data, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T data, IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T data, string userId, CancellationToken cancellationToken = default);
}
