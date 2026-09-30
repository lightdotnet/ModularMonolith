using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Roles.Commands;

internal sealed record UpdateRoleCommand(RoleDto Model) : ICommand<IResult>;

internal class UpdateRoleCommandHandler(IRoleService roleService)
    : ICommandHandler<UpdateRoleCommand, IResult>
{
    public Task<IResult> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken) =>
        roleService.UpdateAsync(request.Model);
}
