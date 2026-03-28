using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Monolith.Identity.Services;

public class RoleService(RoleManager<Role> roleManager) : IRoleService
{
    protected RoleManager<Role> RoleManager => roleManager;

    public virtual async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        return await RoleManager.Roles
            .AsNoTracking()
            .MapToDto()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public virtual Task<IResult<RoleDto>> GetByIdAsync(string id) =>
        GetByAsync(() => RoleManager.FindByIdAsync(id), $"Role {id} not found");

    public virtual Task<IResult<RoleDto>> GetByNameAsync(string name) =>
        GetByAsync(() => RoleManager.FindByNameAsync(name), $"Role {name} not found");

    private async Task<IResult<RoleDto>> GetByAsync(Func<Task<Role?>> getRole, string notFoundMessage)
    {
        var role = await getRole().ConfigureAwait(false);

        if (role is null)
            return Result<RoleDto>.NotFound(notFoundMessage);

        var dto = role.MapToDto();

        var claims = await RoleManager.GetClaimsAsync(role).ConfigureAwait(false);

        dto.Claims = [.. claims
            .Select(c => new ClaimDto
            {
                Type = c.Type,
                Value = c.Value
            })];

        return Result<RoleDto>.Success(dto);
    }

    public virtual async Task<IResult<string>> CreateAsync(CreateRoleRequest request)
    {
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
        };

        var result = await RoleManager.CreateAsync(role).ConfigureAwait(false);

        return result.ToResult(role.Id);
    }

    public virtual async Task<IResult> UpdateAsync(RoleDto request)
    {
        var role = await RoleManager.FindByIdAsync(request.Id).ConfigureAwait(false);

        if (role is null)
            return Result.NotFound($"Role {request.Id} not found");

        role.Update(request.Name, request.Description);

        var result = await RoleManager.UpdateAsync(role).ConfigureAwait(false);

        if (!result.Succeeded)
            return result.ToResult();

        await UpdateClaimsAsync(role, request.Claims).ConfigureAwait(false);

        return Result.Success();
    }

    public virtual async Task UpdateClaimsAsync(Role role, IEnumerable<ClaimDto> claims)
    {
        var existing = await RoleManager.GetClaimsAsync(role).ConfigureAwait(false);

        var target = claims
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();

        await CollectionSyncExtensions.SyncCollectionAsync(
            existing,
            target,
            removeAsync: c => RoleManager.RemoveClaimAsync(role, c),
            addAsync: c => RoleManager.AddClaimAsync(role, c),
            comparer: ClaimComparer.Instance
        ).ConfigureAwait(false);
    }

    public virtual async Task<IResult> DeleteAsync(string id)
    {
        var role = await RoleManager.FindByIdAsync(id).ConfigureAwait(false);

        if (role is null)
            return Result.NotFound($"Role {id} not found");

        var claims = await RoleManager.GetClaimsAsync(role).ConfigureAwait(false);

        if (claims.Count != 0)
            return Result.Error("Role has already setup claims.");

        var result = await RoleManager.DeleteAsync(role).ConfigureAwait(false);

        return result.ToResult();
    }
}
