using StarterKit.Modules.Identity.Application.Common.Models;
using StarterKit.Modules.Identity.Application.Users.Services;

namespace StarterKit.Modules.Identity.Application.Users.Queries;

internal sealed record SearchUserQuery(SearchUserRequest Model)
    : IQuery<PagedResult<UserDto>>;

internal class SearchUserQueryHandler(IUserQueryService userQuery)
    : IQueryHandler<SearchUserQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(
        SearchUserQuery request,
        CancellationToken cancellationToken)
    {
        return await userQuery
            .SearchAsync(request.Model, cancellationToken)
            .ConfigureAwait(false);
    }
}
