using Microsoft.AspNetCore.SignalR;
using StarterKit.Shared.Constants;

namespace StarterKit.Notifications.Api.SignalR;

public class CustomIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId = connection.User?.FindFirst(ClaimTypeConstants.UserId)?.Value;
        return userId;
    }
}