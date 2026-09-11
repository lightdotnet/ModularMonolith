using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;

namespace StarterKit.Identity.Api.Application.Roles.Commands;

internal sealed record CreateRoleCommand(CreateRoleRequest Model) : ICommand<IResult<string>>;

internal class CreateRoleCommandHandler(IRoleService roleService)
    : ICommandHandler<CreateRoleCommand, IResult<string>>
{
    public Task<IResult<string>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken) =>
        roleService.CreateAsync(request.Model);
}
