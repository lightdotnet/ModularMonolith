using StarterKit.Identity.Contracts.Services;

namespace StarterKit.Identity.Api.Application.Users.Commands;

internal sealed record DeleteUserCommand(string Id) : ICommand<IResult>;

internal class DeleteUserCommandHandler(IUserService userService)
    : ICommandHandler<DeleteUserCommand, IResult>
{
    public Task<IResult> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken) =>
        userService.DeleteAsync(request.Id);
}
