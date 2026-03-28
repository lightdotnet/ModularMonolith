namespace Monolith.Identity;

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
}