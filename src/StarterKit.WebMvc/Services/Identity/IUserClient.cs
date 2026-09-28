using Light.Contracts;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

/// <summary>
/// Identity <c>UserController</c> (<c>user</c>) — one method per endpoint.
/// </summary>
public interface IUserClient
{
    /// <summary><c>GET user/search</c></summary>
    Task<ApiResult<Paged<UserDto>>> SearchAsync(
        SearchUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user</c></summary>
    Task<ApiResult<IReadOnlyList<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user/{id}</c></summary>
    Task<ApiResult<UserDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user/by_username/{username}</c></summary>
    Task<ApiResult<UserDto>> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST user</c> — returns the new user's id.</summary>
    Task<ApiResult<string>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT user/{id}</c></summary>
    Task<ApiResult> UpdateAsync(
        UserDto request,
        CancellationToken cancellationToken = default);

    /// <summary><c>DELETE user/{id}</c></summary>
    Task<ApiResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT user/{id}/password/force</c></summary>
    Task<ApiResult> ForcePasswordAsync(
        string id,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user/get_domain_user/{userName}</c></summary>
    Task<ApiResult<DomainUserDto>> GetDomainUserAsync(
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT user/sync_domain_users</c></summary>
    Task<ApiResult> SyncDomainUsersAsync(
        CancellationToken cancellationToken = default);
}
