using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Users.Commands;

internal sealed record UpdateUserCommand(UserDto Model) : ICommand<IResult>;

internal class UpdateUserCommandHandler(
    IUserService userService,
    PermissionGrantGuard guard)
    : ICommandHandler<UpdateUserCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var allowed = await guard.CanUpdateUserAsync(request.Model).ConfigureAwait(false);

        if (!allowed.IsSuccess)
            return allowed;

        return await userService.UpdateAsync(request.Model).ConfigureAwait(false);
    }
}
