using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

/// <summary>
/// Identity <c>UserProfileController</c> (<c>user_profile</c>) — the signed-in user's own profile
/// and sessions. One method per endpoint.
/// </summary>
public interface IUserProfileClient
{
    /// <summary>
    /// <c>GET user_profile</c>. <paramref name="accessToken"/> overrides the session token — used
    /// while a session is being established/refreshed, before the cookie carries the new token.
    /// Returns an <c>unauthorized</c> result (not an exception) when <paramref name="accessToken"/>
    /// is supplied, so sign-in can tolerate a profile failure.
    /// </summary>
    Task<ApiResult<UserDto>> GetAsync(
        string? accessToken = null,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET user_profile/token/list</c> — the user's active sessions.</summary>
    Task<ApiResult<IReadOnlyList<UserSessionDto>>> GetSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT user_profile/token/revoke</c> — revokes one of the user's sessions.</summary>
    Task<ApiResult> RevokeSessionAsync(
        string tokenId,
        CancellationToken cancellationToken = default);
}
