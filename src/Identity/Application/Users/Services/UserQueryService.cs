using Light.Extensions.Caching;
using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Contracts;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Shared;

namespace StarterKit.Modules.Identity.Application.Users.Services;

/// <summary>
/// Serves user reads from one cached copy of the full user list (see <see cref="IUserQueryService"/>).
/// </summary>
/// <remarks>
/// The memory cache provider hands back the cached instances themselves and <see cref="UserDto"/> is
/// mutable, so every read returns fresh copies - a caller can never alter the cached list.
/// </remarks>
internal sealed class UserQueryService(
    IUserService userService,
    ICacheService cacheService,
    ILogger<UserQueryService> logger)
    : IUserQueryService
{
    private const string CacheKey = "identity:users";

    private const int MaxPageSize = 100;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(30);

    private static readonly string ActiveStatusValue = ActiveStatus.State.Active.ToString();

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await GetCachedAsync(ct).ConfigureAwait(false);

        return [.. users.Select(Clone)];
    }

    public async Task<PagedResult<UserDto>> SearchAsync(
        SearchUserRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await GetCachedAsync(ct).ConfigureAwait(false);

        // Only search once the value is within a sane length: too short (<2) is a near-universal
        // match, too long (>256) is an unbounded-input guard.
        var searchValue = request.SearchValue?.Trim();

        IEnumerable<UserDto> query = users;

        if (searchValue is { Length: >= 2 and <= 256 })
            query = query.Where(x => Matches(x, searchValue));

        return ToPagedResult(
            query,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<UserSummary?> GetSummaryAsync(string userId, CancellationToken ct = default)
    {
        var users = await GetCachedAsync(ct).ConfigureAwait(false);

        var user = users.FirstOrDefault(x => x.Id == userId);

        return user is null ? null : ToSummary(user);
    }

    public async Task<IReadOnlyList<UserSummary>> GetSummariesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct = default)
    {
        if (userIds.Count == 0)
            return [];

        var users = await GetCachedAsync(ct).ConfigureAwait(false);

        var ids = userIds.ToHashSet(StringComparer.Ordinal);

        return [.. users
            .Where(x => ids.Contains(x.Id))
            .Select(ToSummary)];
    }

    public async Task<UserSummary?> FindSummaryByEmailAsync(string email, CancellationToken ct = default)
    {
        var users = await GetCachedAsync(ct).ConfigureAwait(false);

        var user = users.FirstOrDefault(x => string.Equals(
            x.Email,
            email,
            StringComparison.OrdinalIgnoreCase));

        return user is null ? null : ToSummary(user);
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        // Runs after a user write has already committed, so a failure here must not surface as a
        // failure of that write: log it and drop the cached list so the next read reloads it.
        try
        {
            var users = await LoadAsync().ConfigureAwait(false);

            await cacheService
                .TrySetAsync(
                    CacheKey,
                    users,
                    CacheLifetime,
                    cancellationToken: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reloading the cached user list {Key} failed.", CacheKey);

            await TryRemoveAsync().ConfigureAwait(false);
        }
    }

    private async Task<List<UserDto>> GetCachedAsync(CancellationToken ct)
    {
        var users = await cacheService
            .TryGetAsync<List<UserDto>>(CacheKey, ct)
            .ConfigureAwait(false);

        if (users is not null)
            return users;

        users = await LoadAsync().ConfigureAwait(false);

        await cacheService
            .TrySetAsync(
                CacheKey,
                users,
                CacheLifetime,
                cancellationToken: ct)
            .ConfigureAwait(false);

        return users;
    }

    private async Task<List<UserDto>> LoadAsync()
    {
        var users = await userService.GetAllAsync().ConfigureAwait(false);

        return [.. users];
    }

    private async Task TryRemoveAsync()
    {
        try
        {
            // Not tied to the request token: the write has committed, so the stale list must go.
            await cacheService
                .RemoveAsync(CacheKey, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Removing the cached user list {Key} failed.", CacheKey);
        }
    }

    private static bool Matches(UserDto user, string value)
    {
        return Contains(user.UserName, value)
            || Contains(user.FirstName, value)
            || Contains(user.LastName, value)
            || Contains($"{user.FirstName} {user.LastName}", value)
            || Contains(user.Email, value)
            || Contains(user.PhoneNumber, value);
    }

    private static bool Contains(string? source, string value) =>
        source is not null && source.Contains(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// In-memory counterpart of <c>QueryableResultExtensions.ToPagedResultAsync</c>, with the same
    /// page-number/page-size normalization.
    /// </summary>
    private static PagedResult<UserDto> ToPagedResult(
        IEnumerable<UserDto> source,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, MaxPageSize);

        var matches = source.ToList();

        var items = matches
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(Clone)
            .ToList();

        return new PagedResult<UserDto>(new Paged<UserDto>(
            items,
            pageNumber,
            pageSize,
            matches.Count));
    }

    private static UserSummary ToSummary(UserDto user) => new(
        user.Id,
        user.UserName ?? string.Empty,
        user.Email ?? string.Empty,
        user.FirstName,
        user.LastName,
        user.Status == ActiveStatusValue);

    private static UserDto Clone(UserDto user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Status = user.Status,
        AuthProvider = user.AuthProvider,
        IsDeleted = user.IsDeleted,
        Roles = [.. user.Roles],
        Claims = [.. user.Claims.Select(c => new ClaimDto
        {
            Type = c.Type,
            Value = c.Value,
        })],
    };
}
