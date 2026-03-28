using Microsoft.AspNetCore.SignalR;
using Monolith.Claims;

namespace Monolith.SignalR;

public class CustomIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId = connection.User?.FindFirst(ClaimTypes.UserId)?.Value;
        return userId;
    }
}