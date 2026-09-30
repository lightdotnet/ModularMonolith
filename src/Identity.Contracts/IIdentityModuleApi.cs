namespace StarterKit.Modules.Identity.Contracts;

/// <summary>
/// In-process seam through which other modules query and provision users owned by the Identity module.
/// </summary>
public interface IIdentityModuleApi
{
    /// <summary>
    /// Returns the user with the given id, or <c>null</c> when it does not exist.
    /// </summary>
    Task<UserSummary?> GetUserAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the users matching the given ids; unknown ids are skipped.
    /// </summary>
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the user with the given email, or <c>null</c> when it does not exist.
    /// </summary>
    Task<UserSummary?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Returns the ids of the users granted the given permission.
    /// </summary>
    Task<IReadOnlyList<string>> GetUserIdsWithPermissionAsync(
        string permission,
        CancellationToken ct = default);

    /// <summary>
    /// Idempotently invites a user by email: returns the id of the existing user with that email,
    /// or provisions a new one and returns its id.
    /// </summary>
    Task<Result<string>> EnsureUserAsync(
        string email,
        string? firstName,
        string? lastName,
        CancellationToken ct = default);
}
