using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StarterKit.Modules.Notifications.Infrastructure.SignalR;

[Authorize]
public class SignalRHub(ILogger<SignalRHub> logger) : Hub
{
    private const string UsersGroup = "SignalR Users";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UsersGroup);

        await base.OnConnectedAsync();

        logger.LogInformation(
            "A client connected to NotificationHub: {connectionId}",
            Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UsersGroup);

        await base.OnDisconnectedAsync(exception);

        logger.LogInformation(
            "A client disconnected from NotificationHub: {connectionId}",
            Context.ConnectionId);
    }
}
