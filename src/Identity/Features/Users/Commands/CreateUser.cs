using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Users.Commands;

internal sealed record CreateUserCommand(CreateUserRequest Model) : ICommand<IResult<string>>;

/// <remarks>
/// The <c>UserProvisionedIntegrationEvent</c> is raised by <see cref="IUserService.CreateAsync"/>
/// and published once the new user is committed.
/// </remarks>
internal class CreateUserCommandHandler(IUserService userService)
    : ICommandHandler<CreateUserCommand, IResult<string>>
{
    public Task<IResult<string>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken) =>
        userService.CreateAsync(request.Model);
}
