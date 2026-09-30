using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Roles.Commands;

internal sealed record UpdateRoleCommand(RoleDto Model) : ICommand<IResult>;

internal class UpdateRoleCommandHandler(
    IRoleService roleService,
    PermissionGrantGuard guard)
    : ICommandHandler<UpdateRoleCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var allowed = await guard.CanUpdateRoleAsync(request.Model).ConfigureAwait(false);

        if (!allowed.IsSuccess)
            return allowed;

        return await roleService.UpdateAsync(request.Model).ConfigureAwait(false);
    }
}
