using StarterKit.Identity.Contracts;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

internal sealed class RoleClient(
    IHttpClientFactory httpClientFactory,
    ILogger<RoleClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Identity),
        logger),
    IRoleClient
{
    public Task<ApiResult<IReadOnlyList<RoleDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<RoleDto>>(
            HttpMethod.Get,
            "role",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<RoleDto>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<RoleDto>(
            HttpMethod.Get,
            $"role/{Segment(id)}",
            cancellationToken: cancellationToken);
    }

    public Task<ApiResult<string>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<string>(
            HttpMethod.Post,
            "role",
            new ApiRequest { Body = request },
            cancellationToken);
    }

    public Task<ApiResult> UpdateAsync(
        RoleDto request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Put,
            "role",
            new ApiRequest { Body = request },
            cancellationToken);
    }

    public Task<ApiResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            HttpMethod.Delete,
            $"role/{Segment(id)}",
            cancellationToken: cancellationToken);
    }
}
