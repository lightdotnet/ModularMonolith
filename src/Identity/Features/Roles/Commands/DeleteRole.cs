using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Features.Roles.Commands;

internal sealed record DeleteRoleCommand(string Id) : ICommand<IResult>;

internal class DeleteRoleCommandHandler(IRoleService roleService)
    : ICommandHandler<DeleteRoleCommand, IResult>
{
    public Task<IResult> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken) =>
        roleService.DeleteAsync(request.Id);
}
