using Light.AspNetCore.Authorization;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

/// <summary>
/// Identity <c>PermissionsController</c> (<c>permissions</c>) — the permission catalog. Returns the
/// vendor <see cref="PermissionDefinition"/> the backend serializes (brought in transitively by
/// the Contracts projects), so no local copy of the type is needed.
/// </summary>
public interface IPermissionClient
{
    /// <summary><c>GET permissions</c></summary>
    Task<ApiResult<IReadOnlyList<PermissionDefinition>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
