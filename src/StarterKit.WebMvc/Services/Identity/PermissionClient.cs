using Light.AspNetCore.Authorization;
using StarterKit.WebMvc.Services.Http;

namespace StarterKit.WebMvc.Services.Identity;

internal sealed class PermissionClient(
    IHttpClientFactory httpClientFactory,
    ILogger<PermissionClient> logger)
    : ApiClientBase(
        httpClientFactory.CreateClient(ApiClientNames.Identity),
        logger),
    IPermissionClient
{
    public Task<ApiResult<IReadOnlyList<PermissionDefinition>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<PermissionDefinition>>(
            HttpMethod.Get,
            "permissions",
            cancellationToken: cancellationToken);
    }
}
