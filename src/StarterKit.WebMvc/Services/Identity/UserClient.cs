using System.Globalization;
using Light.Contracts;
using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

internal sealed class UserClient(
    IHttpClientFactory httpClientFactory,
    ILogger<UserClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Identity),
        logger),
    IUserClient
{
    public Task<ApiResult<Paged<UserDto>>> SearchAsync(
        SearchUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<Paged<UserDto>>(
            HttpMethod.Get,
            "user/search",
            new ApiRequest
            {
                Query = new Dictionary<string, string?>
                {
                    ["searchValue"] = request.SearchValue,
                    ["pageNumber"] = request.PageNumber.ToString(CultureInfo.InvariantCulture),
                    ["pageSize"] = request.PageSize.ToString(CultureInfo.InvariantCulture),
                    ["sortBy"] = request.SortBy,
                    ["sortDirection"] = request.SortBy is null ? null : request.SortDirection,
                },
            },
            cancellationToken);
    }

    public Task<ApiResult<IReadOnlyList<UserDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<UserDto>>(
            HttpMethod.Get,
            "user",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<UserDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserDto>(
            HttpMethod.Get,
            $"user/{Segment(id)}",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<UserDto>> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserDto>(
            HttpMethod.Get,
            $"user/by_username/{Segment(userName)}",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<string>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<string>(
            HttpMethod.Post,
            "user",
            new ApiRequest { Body = request },
            cancellationToken);
    }

    public Task<ApiResult> UpdateAsync(
        UserDto request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Put,
            $"user/{Segment(request.Id)}",
            new ApiRequest { Body = request },
            cancellationToken);
    }

    public Task<ApiResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Delete,
            $"user/{Segment(id)}",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult> ForcePasswordAsync(
        string id,
        string password,
        CancellationToken cancellationToken = default)
    {
        // The endpoint binds [FromBody] string, so the body is a bare JSON string.
        return SendAsync(
            HttpMethod.Put,
            $"user/{Segment(id)}/password/force",
            new ApiRequest { Body = password },
            cancellationToken);
    }

    public Task<ApiResult<DomainUserDto>> GetDomainUserAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<DomainUserDto>(
            HttpMethod.Get,
            $"user/get_domain_user/{Segment(userName)}",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult> SyncDomainUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Put,
            "user/sync_domain_users",
            cancellationToken: cancellationToken);
    }
}
