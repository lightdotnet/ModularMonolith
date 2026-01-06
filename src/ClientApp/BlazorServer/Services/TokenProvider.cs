using Monolith.HttpApi.Common.Interfaces;

namespace Monolith.Blazor.Services;

public class TokenProvider(TokenStorage tokenStorage) : ITokenProvider
{
    public async Task<string?> GetAccessTokenAsync()
    {
        var getToken = await tokenStorage.GetAsync();
        return getToken?.Token;
    }
}
