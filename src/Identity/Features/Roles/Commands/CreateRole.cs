using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Roles.Commands;

internal sealed record CreateRoleCommand(CreateRoleRequest Model) : ICommand<IResult<string>>;

internal class CreateRoleCommandHandler(IRoleService roleService)
    : ICommandHandler<CreateRoleCommand, IResult<string>>
{
    public Task<IResult<string>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken) =>
        roleService.CreateAsync(request.Model);
}
