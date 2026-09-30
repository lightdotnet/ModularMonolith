using Microsoft.AspNetCore.Identity;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.Models;
using StarterKit.Modules.Identity.Services;
using StarterKit.Shared;
using StarterKit.Shared.Authorization;
using StarterKit.Shared.Constants;
using System.Security.Claims;

namespace StarterKit.Modules.Identity.Authorization;

/// <summary>
/// Privilege-escalation guard for the user and role write commands. An actor may only grant or
/// revoke permissions they hold (directly on a user, through a role assigned to or removed from a
/// user, or on a role's claims), may only manage users whose effective permissions they hold, and
/// may not change their own roles, direct claims or status. A full-control actor bypasses these
/// checks, except the delete rules (nobody deletes their own account or a super user).
/// </summary>
/// <remarks>
/// Diffs are computed against the persisted state. A missing target passes through, so the
/// service called next produces its usual not-found result.
/// </remarks>
internal sealed class PermissionGrantGuard(
    ICurrentUser currentUser,
    UserManager<User> userManager,
    RoleManager<Role> roleManager)
{
    public async Task<IResult> CanUpdateUserAsync(UserDto model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (currentUser.IsFullControl())
            return Result.Success();

        var user = await userManager
            .FindByIdAsync(model.Id)
            .ConfigureAwait(false);

        if (user is null)
            return Result.Success();

        var currentRoles = await userManager
            .GetRolesAsync(user)
            .ConfigureAwait(false);

        var currentClaims = await userManager
            .GetClaimsAsync(user)
            .ConfigureAwait(false);

        var targetRoles = (model.Roles ?? []).ToHashSet(StringComparer.Ordinal);

        var targetClaims = (model.Claims ?? [])
            .Select(c => new Claim(c.Type, c.Value))
            .ToHashSet(ClaimComparer.Instance);

        var addedRoles = targetRoles.Except(currentRoles, StringComparer.Ordinal).ToList();
        var removedRoles = currentRoles.Except(targetRoles, StringComparer.Ordinal).ToList();

        if (IsSelf(user.Id))
        {
            if (addedRoles.Count != 0 || removedRoles.Count != 0)
                return Result.Error("You cannot change your own roles.");

            if (!currentClaims.ToHashSet(ClaimComparer.Instance).SetEquals(targetClaims))
                return Result.Error("You cannot change your own claims.");

            if (IsStatusChange(user, model.Status))
                return Result.Error("You cannot change your own status.");

            return Result.Success();
        }

        var managed = await CanManageAsync(user).ConfigureAwait(false);

        if (!managed.IsSuccess)
            return managed;

        // Permissions granted or revoked through the roles assigned to / removed from the user.
        var changedPermissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in addedRoles.Concat(removedRoles))
            changedPermissions.UnionWith(await GetRolePermissionsAsync(roleName).ConfigureAwait(false));

        // Permissions granted or revoked directly on the user.
        var currentPermissions = PermissionsOf(currentClaims);
        var targetPermissions = PermissionsOf(targetClaims);

        changedPermissions.UnionWith(targetPermissions.Except(currentPermissions));
        changedPermissions.UnionWith(currentPermissions.Except(targetPermissions));

        return RequireHeld(
            changedPermissions,
            "You cannot grant or revoke permissions you do not hold");
    }

    public async Task<IResult> CanDeleteUserAsync(string id)
    {
        if (IsSelf(id))
            return Result.Error("You cannot delete your own account.");

        var user = await userManager
            .FindByIdAsync(id)
            .ConfigureAwait(false);

        if (user is null)
            return Result.Success();

        if (SuperUserPolicy.IsSuper(user.UserName))
            return Result.Error($"The super user {user.UserName} cannot be deleted.");

        if (currentUser.IsFullControl())
            return Result.Success();

        return await CanManageAsync(user).ConfigureAwait(false);
    }

    public async Task<IResult> CanForcePasswordAsync(string id)
    {
        if (currentUser.IsFullControl() || IsSelf(id))
            return Result.Success();

        var user = await userManager
            .FindByIdAsync(id)
            .ConfigureAwait(false);

        if (user is null)
            return Result.Success();

        return await CanManageAsync(user).ConfigureAwait(false);
    }

    public async Task<IResult> CanUpdateRoleAsync(RoleDto model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (currentUser.IsFullControl())
            return Result.Success();

        var role = await roleManager
            .FindByIdAsync(model.Id)
            .ConfigureAwait(false);

        if (role is null)
            return Result.Success();

        var currentPermissions = await GetRolePermissionsAsync(role).ConfigureAwait(false);

        var held = RequireHeld(
            currentPermissions,
            $"You cannot modify the role {role.Name} because it grants permissions you do not hold");

        if (!held.IsSuccess)
            return held;

        // Every current permission is held, so only the added ones remain to check.
        var targetPermissions = PermissionsOf((model.Claims ?? [])
            .Select(c => new Claim(c.Type, c.Value)));

        return RequireHeld(
            targetPermissions.Except(currentPermissions),
            "You cannot grant permissions you do not hold");
    }

    public async Task<IResult> CanDeleteRoleAsync(string id)
    {
        if (currentUser.IsFullControl())
            return Result.Success();

        var role = await roleManager
            .FindByIdAsync(id)
            .ConfigureAwait(false);

        if (role is null)
            return Result.Success();

        var currentPermissions = await GetRolePermissionsAsync(role).ConfigureAwait(false);

        return RequireHeld(
            currentPermissions,
            $"You cannot delete the role {role.Name} because it grants permissions you do not hold");
    }

    /// <summary>
    /// The actor may manage <paramref name="user"/> only when they hold every permission the
    /// user holds; a super user (implicit full control) is managed by full-control actors only.
    /// </summary>
    private async Task<IResult> CanManageAsync(User user)
    {
        if (SuperUserPolicy.IsSuper(user.UserName))
            return Result.Error($"You cannot manage the super user {user.UserName}.");

        var permissions = await GetEffectivePermissionsAsync(user).ConfigureAwait(false);

        return RequireHeld(
            permissions,
            $"You cannot manage {user.UserName} because they hold permissions you do not hold");
    }

    private async Task<HashSet<string>> GetEffectivePermissionsAsync(User user)
    {
        var claims = await userManager
            .GetClaimsAsync(user)
            .ConfigureAwait(false);

        var permissions = PermissionsOf(claims);

        var roles = await userManager
            .GetRolesAsync(user)
            .ConfigureAwait(false);

        foreach (var roleName in roles)
            permissions.UnionWith(await GetRolePermissionsAsync(roleName).ConfigureAwait(false));

        return permissions;
    }

    private async Task<HashSet<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await roleManager
            .FindByNameAsync(roleName)
            .ConfigureAwait(false);

        return role is null
            ? []
            : await GetRolePermissionsAsync(role).ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetRolePermissionsAsync(Role role)
    {
        var claims = await roleManager
            .GetClaimsAsync(role)
            .ConfigureAwait(false);

        return PermissionsOf(claims);
    }

    private IResult RequireHeld(
        IEnumerable<string> permissions,
        string message)
    {
        var missing = permissions
            .Where(p => !currentUser.HasPermission(p))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return missing.Count == 0
            ? Result.Success()
            : Result.Error($"{message}: {string.Join(", ", missing)}.");
    }

    private bool IsSelf(string? userId) =>
        !string.IsNullOrEmpty(currentUser.UserId)
        && string.Equals(currentUser.UserId, userId, StringComparison.Ordinal);

    /// <summary>
    /// Mirrors <see cref="UserService.UpdateAsync"/>: only a parsable Active/Locked status is applied.
    /// </summary>
    private static bool IsStatusChange(
        User user,
        string? status) =>
        Enum.TryParse<ActiveStatus.State>(status, out var state)
        && state is ActiveStatus.State.Active or ActiveStatus.State.Locked
        && state != user.Status.Value;

    private static HashSet<string> PermissionsOf(IEnumerable<Claim> claims) =>
        claims
            .Where(c => c.Type == ClaimTypeConstants.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
}
