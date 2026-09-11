using Microsoft.AspNetCore.Identity;
using StarterKit.Identity.Api.Entities;
using StarterKit.Identity.Contracts;

namespace StarterKit.Identity.Api.Application.Users.Queries;

internal sealed record SearchUserQuery(SearchUserRequest Model) : IQuery<PagedResult<UserDto>>;

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

        return await userManager.Users
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
                )
            .OrderByDescending(x => x.Created)
            .ThenBy(x => x.UserName)
            .MapToDto()
            .ToPagedResultAsync(request.Model, cancellationToken)
            .ConfigureAwait(false);
    }
}
