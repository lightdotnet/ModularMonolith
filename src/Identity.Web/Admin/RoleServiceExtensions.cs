using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;

namespace StarterKit.Modules.Identity.Web.Admin;

internal static class RoleServiceExtensions
{
    /// <summary>
    /// All roles, ordered by name, each with its claims. Roles are few, so this loads them one
    /// by one through <see cref="IRoleService.GetByIdAsync"/> (sequentially: one scoped context).
    /// </summary>
    public static async Task<IReadOnlyList<RoleDto>> GetAllWithClaimsAsync(this IRoleService roleService)
    {
        var roles = await roleService.GetAllAsync();
        var result = new List<RoleDto>();

        foreach (var role in roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            var detail = await roleService.GetByIdAsync(role.Id);
            result.Add(detail.IsSuccess && detail.Data is not null ? detail.Data : role);
        }

        return result;
    }
}
