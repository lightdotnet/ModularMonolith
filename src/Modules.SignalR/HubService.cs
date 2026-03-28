using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Monolith.Notifications;

namespace Monolith.SignalR;

internal class HubService : IHubService
{
    private readonly IHubContext<SignalRHub> _hubContext;
    private readonly ILogger<HubService> _logger;

    public HubService(IHubContext<SignalRHub> notificationHubContext, ILogger<HubService> logger) =>
        (_hubContext, _logger) = (notificationHubContext, logger);

    public Task NotifyAsync(CancellationToken cancellationToken = default) =>
        _hubContext.Clients.All.SendAsync(
            NotificationConstants.SERVER_NOTIFICATION, cancellationToken);

    public Task NotifyAsync(string userId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.User(userId).SendAsync(
            NotificationConstants.SERVER_NOTIFICATION, cancellationToken);

    public Task NotifyAsync(IEnumerable<string> userIds,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Users(userIds).SendAsync(
            NotificationConstants.SERVER_NOTIFICATION, cancellationToken);

    public Task SendAsync<T>(T data, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.All.SendAsync(typeof(T).Name, data, cancellationToken);

    public Task SendAsync<T>(T data, string userId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.User(userId).SendAsync(typeof(T).Name, data, cancellationToken);

    public Task SendAsync<T>(T data, IEnumerable<string> userIds, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Users(userIds).SendAsync(typeof(T).Name, data, cancellationToken);
}
