namespace StarterKit.Identity.Contracts.Services;

public interface IUserService
{
    /// <summary>
    /// Get all users
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllAsync();

    /// <summary>
    /// Get user by Id
    /// </summary>
    Task<IResult<UserDto>> GetByIdAsync(string id);

    /// <summary>
    /// Get user by UserName
    /// </summary>
    Task<IResult<UserDto>> GetByUserNameAsync(string userName);

    /// <summary>
    /// Create user
    /// </summary>
    Task<IResult<string>> CreateAsync(CreateUserRequest newUser);

    /// <summary>
    /// Update user
    /// </summary>
    Task<IResult> UpdateAsync(UserDto updateUser);

    /// <summary>
    /// Delete user
    /// </summary>
    Task<IResult> DeleteAsync(string id);

    /// <summary>
    /// Force change password with auto generate reset token for user 
    /// </summary>
    Task<IResult> ForcePasswordAsync(string id, string password);

    /// <summary>
    /// Get users who have the claim
    /// </summary>
    Task<IEnumerable<UserDto>> GetUsersHasClaimAsync(string claimType, string claimValue);

    /// <summary>
    /// Set (or clear, when <paramref name="claimValue"/> is null) a single-valued claim on a user,
    /// replacing any existing claim of the same type. Used by other modules to stamp an opaque
    /// cross-module reference (e.g. Organization's employee id) onto a user without Identity
    /// needing to know what the claim represents.
    /// </summary>
    Task<IResult> SetClaimAsync(string userId, string claimType, string? claimValue);
}