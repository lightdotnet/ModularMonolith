using Microsoft.AspNetCore.SignalR;
using StarterKit.Shared.Constants;

namespace StarterKit.Modules.Notifications.Infrastructure.SignalR;

/// <summary>
/// Resolves a SignalR connection's user id from the application's user-id claim, so pushes
/// addressed with <c>Clients.User(userId)</c> reach every connection of that user.
/// </summary>
internal sealed class CustomIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(ClaimTypeConstants.UserId)?.Value;
}
