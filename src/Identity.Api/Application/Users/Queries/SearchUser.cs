using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.Application.Users.Queries;

internal sealed record SearchUserQuery(SearchUserRequest Model) : IQuery<PagedResult<UserDto>>;

internal sealed class SearchUserQueryValidator : AbstractValidator<SearchUserQuery>
{
    public SearchUserQueryValidator()
    {
        RuleFor(x => x.Model).SetValidator(new SearchUserRequestValidator());
    }
}

internal class SearchUserQueryHandler(
    UserManager<User> userManager)
    : IQueryHandler<SearchUserQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(
        SearchUserQuery request, CancellationToken cancellationToken)
    {
        // Only search once the value is within a sane length: too short (<2) is a near-universal
        // match not worth the 5-column scan, too long (>256) is an unbounded-input guard.
        var searchValue = request.Model.SearchValue?.Trim();
        var hasSearch = searchValue is { Length: >= 2 and <= 256 };

        var filtered = userManager.Users
            .AsNoTracking()
            // Contains() case-sensitivity depends on the active provider's default collation
            // (case-insensitive on SQL Server, case-sensitive on PostgreSQL) - normalize
            // explicitly (e.g. EF.Functions.ILike on PostgreSQL) if that needs to be consistent.
            .WhereIf(
                hasSearch,
                x =>
                    x.UserName!.Contains(searchValue!)
                    || x.FirstName!.Contains(searchValue!)
                    || x.LastName!.Contains(searchValue!)
                    || ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Contains(searchValue!)
                    || x.Email!.Contains(searchValue!)
                    || x.PhoneNumber!.Contains(searchValue!)
                );

        return await ApplySort(
                filtered,
                request.Model.SortBy,
                request.Model.SortDirection)
            .MapToDto()
            .ToPagedResultAsync(request.Model, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the requested whitelisted sort, with the id as a stable tie-breaker for paging.
    /// An absent (or, defensively, unknown) sort field keeps the default ordering: newest first, then user name.
    /// </summary>
    private static IQueryable<User> ApplySort(
        IQueryable<User> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(
            sortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<User>? ordered = sortBy?.Trim().ToLowerInvariant() switch
        {
            "username" => OrderBy(query, x => x.UserName, descending),
            "firstname" => OrderBy(query, x => x.FirstName, descending),
            "lastname" => OrderBy(query, x => x.LastName, descending),
            "fullname" => descending
                ? query
                    .OrderByDescending(x => x.FirstName)
                    .ThenByDescending(x => x.LastName)
                : query
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName),
            "email" => OrderBy(query, x => x.Email, descending),
            "created" => OrderBy(query, x => x.Created, descending),
            "status" => OrderBy(query, x => x.Status.Value, descending),
            _ => null,
        };

        if (ordered is null)
        {
            return query
                .OrderByDescending(x => x.Created)
                .ThenBy(x => x.UserName);
        }

        return ordered.ThenBy(x => x.Id);
    }

    private static IOrderedQueryable<User> OrderBy<TKey>(
        IQueryable<User> query,
        Expression<Func<User, TKey>> keySelector,
        bool descending) =>
        descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
}
