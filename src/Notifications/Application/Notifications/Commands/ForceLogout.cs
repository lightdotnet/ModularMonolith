using StarterKit.Modules.Notifications.Application.Common;

namespace StarterKit.Modules.Notifications.Application.Notifications.Commands;

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
