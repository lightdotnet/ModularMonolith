namespace Monolith.SignalR;

public interface IHubService
{
    Task NotifyAsync(CancellationToken cancellationToken = default);

    Task NotifyAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    Task NotifyAsync(string userId, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T data, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T data, IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    Task SendAsync<T>(T data, string userId, CancellationToken cancellationToken = default);
}