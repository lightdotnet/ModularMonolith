using StarterKit.Modules.Identity.Application.Authorization;
using StarterKit.Modules.Identity.Application.Users.Services;

namespace StarterKit.Modules.Identity.Application.Users.Commands;

internal sealed record ForcePasswordCommand(string Id, string Password) : ICommand<IResult>;

internal class ForcePasswordCommandHandler(
    IUserService userService,
    PermissionGrantGuard guard)
    : ICommandHandler<ForcePasswordCommand, IResult>
{
    public async Task<IResult> Handle(
        ForcePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var allowed = await guard.CanForcePasswordAsync(request.Id).ConfigureAwait(false);

        if (!allowed.IsSuccess)
            return allowed;

        return await userService
            .ForcePasswordAsync(request.Id, request.Password)
            .ConfigureAwait(false);
    }
}
