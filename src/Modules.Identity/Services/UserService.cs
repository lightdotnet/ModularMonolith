using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Monolith.Identity.Services;

public class UserService(UserManager<User> userManager) : IUserService
{
    protected UserManager<User> UserManager => userManager;

    public virtual async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await userManager.Users
            .AsNoTracking()
            .OrderByDescending(x => x.Created)
            .ThenBy(x => x.UserName)
            .MapToDto()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    private async Task<UserDto> GetByAsync(User user)
    {
        var dto = user.MapToDto();

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var claims = await userManager.GetClaimsAsync(user).ConfigureAwait(false);

        dto.Roles = roles;

        dto.Claims = [..claims.Select(s => new ClaimDto
        {
            Type = s.Type,
            Value = s.Value
        })];

        return dto;
    }

    private async Task UpdateRolesAsync(User user, IEnumerable<string> roles)
    {
        var userRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        await CollectionSyncExtensions.SyncCollectionAsync(
            userRoles,
            roles,
            removeAsync: r => userManager.RemoveFromRolesAsync(user, r),
            addAsync: r => userManager.AddToRolesAsync(user, r)
        ).ConfigureAwait(false);
    }

    private async Task UpdateClaimsAsync(User user, IEnumerable<Claim> claims)
    {
        var userClaims = await userManager.GetClaimsAsync(user).ConfigureAwait(false);

        await CollectionSyncExtensions.SyncCollectionAsync(
            userClaims,
            claims,
            removeAsync: c => userManager.RemoveClaimsAsync(user, c),
            addAsync: c => userManager.AddClaimsAsync(user, c),
            comparer: ClaimComparer.Instance
        ).ConfigureAwait(false);
    }

    public virtual async Task<IResult<UserDto>> GetByIdAsync(string id)
    {
        var user = await userManager
            .FindByIdAsync(id)
            .ConfigureAwait(false);

        if (user == null)
            return Result<UserDto>.NotFound($"User {id} not found");

        var dto = await GetByAsync(user).ConfigureAwait(false);

        return Result<UserDto>.Success(dto);
    }

    public virtual async Task<IResult<UserDto>> GetByUserNameAsync(string userName)
    {
        var user = await userManager
            .FindByNameAsync(userName)
            .ConfigureAwait(false);

        if (user == null)
            return Result<UserDto>.NotFound($"User {userName} not found");

        var dto = await GetByAsync(user).ConfigureAwait(false);

        return Result<UserDto>.Success(dto);
    }

    public virtual async Task<IResult<string>> CreateAsync(CreateUserRequest newUser)
    {
        var entity = new User
        {
            UserName = newUser.UserName,
            Email = newUser.Email,
            PhoneNumber = newUser.PhoneNumber,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            AuthProvider = newUser.AuthProvider,
        };

        var identityResult = string.IsNullOrWhiteSpace(newUser.Password)
            ? await userManager
                .CreateAsync(entity)
                .ConfigureAwait(false)
            : await userManager
                .CreateAsync(entity, newUser.Password)
                .ConfigureAwait(false);

        return identityResult.ToResult(entity.Id);
    }

    public virtual async Task<IResult> UpdateAsync(UserDto updateUser)
    {
        ArgumentNullException.ThrowIfNull(updateUser);

        var user = await userManager
            .FindByIdAsync(updateUser.Id)
            .ConfigureAwait(false);

        if (user == null)
            return Result.NotFound($"User {updateUser.Id} not found");

        // update base info
        user.UpdateInfo(
            updateUser.FirstName,
            updateUser.LastName,
            updateUser.PhoneNumber,
            updateUser.Email);

        // update status
        user.UpdateStatus(updateUser.Status);

        // update auth provider
        user.ChangeAuthProvider(updateUser.AuthProvider);

        var updatedResult = await userManager
            .UpdateAsync(user)
            .ConfigureAwait(false);

        if (!updatedResult.Succeeded)
            return updatedResult.ToResult();

        // materialize roles once
        var roles = updateUser.Roles?.ToArray() ?? [];

        // materialize + normalize claims once
        var claims = updateUser.Claims?
            .Select(c => new Claim(c.Type, c.Value))
            .Distinct(new ClaimComparer())
            .ToArray() ?? [];

        await UpdateRolesAsync(user, roles).ConfigureAwait(false);
        await UpdateClaimsAsync(user, claims).ConfigureAwait(false);

        return Result.Success();
    }

    public virtual async Task<IResult> DeleteAsync(string id)
    {
        var user = await userManager
            .FindByIdAsync(id)
            .ConfigureAwait(false);

        if (user == null)
            return Result.NotFound($"User {id} not found");

        user.Delete();

        var identityResult = await userManager
            .DeleteAsync(user)
            .ConfigureAwait(false);

        return identityResult.ToResult();
    }

    public virtual async Task<IResult> ForcePasswordAsync(string id, string password)
    {
        var user = await userManager
            .FindByIdAsync(id)
            .ConfigureAwait(false);

        if (user == null)
            return Result.NotFound($"User {id} not found");

        var token = await userManager
            .GeneratePasswordResetTokenAsync(user)
            .ConfigureAwait(false);

        var identityResult = await userManager
            .ResetPasswordAsync(user, token, password)
            .ConfigureAwait(false);

        return identityResult.ToResult();
    }

    public virtual async Task<IEnumerable<UserDto>> GetUsersHasClaimAsync(string claimType, string claimValue)
    {
        var users = await userManager
            .GetUsersForClaimAsync(new Claim(claimType, claimValue))
            .ConfigureAwait(false);

        return users.Select(s => s.MapToDto());
    }
}
