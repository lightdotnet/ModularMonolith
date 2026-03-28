using Monolith.Identity;

namespace Monolith.HttpApi.Identity;

public class TokenHttpService(IHttpClientFactory httpClientFactory)
    : TryHttpClient(httpClientFactory)
{
    private const string BasePath = "oauth/token";

    public Task<Result<TokenDto>> GetTokenAsync(string username, string password)
    {
        var url = $"{BasePath}/get";

        var request = new { username, password };

        return TryPostAsync<TokenDto>(url, request);
    }

    public Task<Result<TokenDto>> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var url = $"{BasePath}/refresh";

        var request = new { accessToken, refreshToken };

        return TryPostAsync<TokenDto>(url, request);
    }

    public Task<Result<bool>> CheckTokenAsync()
    {
        var url = $"{BasePath}/check";
        return TryGetAsync<bool>(url);
    }
}
