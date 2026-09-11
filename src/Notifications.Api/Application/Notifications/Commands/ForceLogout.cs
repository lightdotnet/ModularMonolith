using StarterKit.Notifications.Api.SignalR;
using StarterKit.Notifications.Contracts.SystemNotifications;

namespace StarterKit.Notifications.Api.Application.Notifications.Commands;

internal sealed record ForceLogoutCommand(ForceLogoutMessage Message) : ICommand<IResult>;

internal class ForceLogoutCommandHandler(IHubService hub)
    : ICommandHandler<ForceLogoutCommand, IResult>
{
    public async Task<IResult> Handle(
        ForceLogoutCommand request,
        CancellationToken cancellationToken)
    {
        // Live session-invalidation signal only - no stored notification record.
        await hub.SendAsync(request.Message, request.Message.UserId, cancellationToken);

        return Result.Success();
    }
}
