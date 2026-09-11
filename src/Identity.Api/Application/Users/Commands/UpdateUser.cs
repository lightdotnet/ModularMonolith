using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;

namespace StarterKit.Identity.Api.Application.Users.Commands;

internal sealed record UpdateUserCommand(UserDto Model) : ICommand<IResult>;

internal class UpdateUserCommandHandler(IUserService userService)
    : ICommandHandler<UpdateUserCommand, IResult>
{
    public Task<IResult> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken) =>
        userService.UpdateAsync(request.Model);
}
