using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Roles.Commands;

internal sealed record DeleteRoleCommand(string Id) : ICommand<IResult>;

internal class DeleteRoleCommandHandler(
    IRoleService roleService,
    PermissionGrantGuard guard)
    : ICommandHandler<DeleteRoleCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var allowed = await guard.CanDeleteRoleAsync(request.Id).ConfigureAwait(false);

        if (!allowed.IsSuccess)
            return allowed;

        return await roleService.DeleteAsync(request.Id).ConfigureAwait(false);
    }
}
