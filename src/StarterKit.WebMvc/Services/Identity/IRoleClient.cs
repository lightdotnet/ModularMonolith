using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

/// <summary>
/// Identity <c>RoleController</c> (<c>role</c>) — one method per endpoint.
/// </summary>
public interface IRoleClient
{
    /// <summary><c>GET role</c></summary>
    Task<ApiResult<IReadOnlyList<RoleDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary><c>GET role/{id}</c></summary>
    Task<ApiResult<RoleDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST role</c> — returns the new role's id.</summary>
    Task<ApiResult<string>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT role</c></summary>
    Task<ApiResult> UpdateAsync(
        RoleDto request,
        CancellationToken cancellationToken = default);

    /// <summary><c>DELETE role/{id}</c></summary>
    Task<ApiResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);
}
