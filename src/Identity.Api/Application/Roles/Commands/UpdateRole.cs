using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;

namespace StarterKit.Identity.Api.Application.Roles.Commands;

internal sealed record UpdateRoleCommand(RoleDto Model) : ICommand<IResult>;

internal class UpdateRoleCommandHandler(IRoleService roleService)
    : ICommandHandler<UpdateRoleCommand, IResult>
{
    public Task<IResult> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken) =>
        roleService.UpdateAsync(request.Model);
}
