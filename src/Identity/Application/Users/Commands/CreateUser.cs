using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Users.Services;

namespace StarterKit.Modules.Identity.Application.Users.Commands;

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
