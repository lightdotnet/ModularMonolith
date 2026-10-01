using StarterKit.Modules.Notifications.Application.Notifications.Commands;
using StarterKit.Shared;

namespace StarterKit.Modules.Notifications.Api;

/// <summary>
/// In-process implementation of the Notifications module's cross-module seam. Each call dispatches
/// the same mediator command the module's endpoints use.
/// </summary>
/// <remarks>
/// Cross-module calls bypass the endpoints' permission attributes; the calling module is
/// responsible for authorizing the operation before it calls this seam.
/// </remarks>
internal sealed class NotificationsModuleApi(
    IMediator mediator,
    ICurrentUser currentUser)
    : INotificationsModuleApi
{
    public async Task SendAsync(
        string toUserId,
        SystemMessage message,
        CancellationToken ct = default)
    {
        await mediator.Send(
            new SendNotificationCommand(
                toUserId,
                message,
                currentUser.UserId),
            ct);
    }
}
