using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Users.Commands;

internal sealed record UpdateUserCommand(UserDto Model) : ICommand<IResult>;

internal class UpdateUserCommandHandler(IUserService userService)
    : ICommandHandler<UpdateUserCommand, IResult>
{
    public Task<IResult> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken) =>
        userService.UpdateAsync(request.Model);
}
