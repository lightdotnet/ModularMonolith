using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Users.Commands;

internal sealed record DeleteUserCommand(string Id) : ICommand<IResult>;

internal class DeleteUserCommandHandler(
    IUserService userService,
    PermissionGrantGuard guard)
    : ICommandHandler<DeleteUserCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var allowed = await guard.CanDeleteUserAsync(request.Id).ConfigureAwait(false);

        if (!allowed.IsSuccess)
            return allowed;

        return await userService.DeleteAsync(request.Id).ConfigureAwait(false);
    }
}
