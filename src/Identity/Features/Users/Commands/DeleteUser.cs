using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Users.Commands;

internal sealed record DeleteUserCommand(string Id) : ICommand<IResult>;

internal class DeleteUserCommandHandler(IUserService userService)
    : ICommandHandler<DeleteUserCommand, IResult>
{
    public Task<IResult> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken) =>
        userService.DeleteAsync(request.Id);
}
