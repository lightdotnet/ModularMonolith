using StarterKit.Identity.Contracts.Services;

namespace StarterKit.Identity.Api.Application.Users.Commands;

internal sealed record ForcePasswordCommand(
    string Id,
    string Password) : ICommand<IResult>;

internal class ForcePasswordCommandHandler(IUserService userService)
    : ICommandHandler<ForcePasswordCommand, IResult>
{
    public Task<IResult> Handle(
        ForcePasswordCommand request,
        CancellationToken cancellationToken) =>
        userService.ForcePasswordAsync(request.Id, request.Password);
}
