using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Contracts;

namespace StarterKit.Modules.Identity.Application.Users.Services;

/// <summary>
/// Read side of the user list, served from one cached snapshot of every user (the result of
/// <see cref="IUserService.GetAllAsync"/>: no roles or claims, ordered by creation date descending
/// then user name). The snapshot is reloaded after every successful user create, update or delete.
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    /// Get all users.
    /// </summary>
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Search users by user name, first/last/full name, email or phone number, paged.
    /// </summary>
    Task<PagedResult<UserDto>> SearchAsync(
        SearchUserRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get the summary of a user by id, or <c>null</c> when no such user exists.
    /// </summary>
    Task<UserSummary?> GetSummaryAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Get the summaries of the given users; unknown ids are skipped.
    /// </summary>
    Task<IReadOnlyList<UserSummary>> GetSummariesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default);

    /// <summary>
    /// Find the summary of a user by email (case-insensitive), or <c>null</c> when none matches.
    /// </summary>
    Task<UserSummary?> FindSummaryByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Reload the cached user list from the store. Never throws: on failure the cached list is
    /// dropped so the next read loads it again.
    /// </summary>
    Task ReloadAsync(CancellationToken ct = default);
}
