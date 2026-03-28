using Monolith.Identity;

namespace Monolith.HttpApi.Identity;

public class UserHttpService(IHttpClientFactory httpClientFactory) :
    TryHttpClient(httpClientFactory)
{
    protected override string ClientName => HttpClientConstants.IdentityApi;

    private const string _path = "user";

    public Task<Result<IEnumerable<UserDto>>> GetAsync()
    {
        var url = _path;

        return TryGetAsync<IEnumerable<UserDto>>(url);
    }

    public Task<Result<UserDto>> GetByIdAsync(string id)
    {
        var url = $"{_path}/{id}";

        return TryGetAsync<UserDto>(url);
    }

    public Task<string> ExportAsync()
    {
        var url = $"{_path}/export";

        return DownloadAsBase64Async(url);
    }

    public Task<Result> CreateAsync(CreateUserRequest request)
    {
        var url = _path;

        return TryPostAsync(url, request);
    }

    public Task<Result> UpdateAsync(UserDto request)
    {
        var url = $"{_path}/{request.Id}";

        return TryPutAsync(url, request);
    }

    public Task<Result> DeleteAsync(string id)
    {
        var url = $"{_path}/{id}";

        return TryDeleteAsync(url);
    }

    public Task<Result> ForcePasswordAsync(string id, string password)
    {
        var url = $"{_path}/{id}/password/force";

        return TryPutAsync(url, password);
    }

    public Task<Result<UserDto>> GetDomainUserAsync(string username)
    {
        var url = $"{_path}/get_domain_user/{username}";

        return TryGetAsync<UserDto>(url);
    }
}