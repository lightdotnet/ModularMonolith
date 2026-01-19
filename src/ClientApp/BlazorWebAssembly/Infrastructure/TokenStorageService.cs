using Light.Blazor;
using Monolith.Blazor.Services;

namespace Monolith.Blazor.Infrastructure;

public class TokenStorageService(IStorageService storageService) : TokenStorage
{
    private const string TOKEN_CACHE_KEY = "__user";

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private async Task<TokenModel?> TryGetTokenCachedAsync()
    {
        try
        {
            var dataAsString = await storageService.GetAsync<string>(TOKEN_CACHE_KEY);

            return TokenModel.ReadFrom(dataAsString);
        }
        catch
        {

        }

        return null;
    }

    public override async Task<TokenModel?> GetAsync()
    {
        var savedToken = await TryGetTokenCachedAsync();

        return savedToken;
    }

    public override async Task SaveAsync(TokenModel token)
    {
        await storageService.SetAsync(TOKEN_CACHE_KEY, token.ToString());
    }

    public override async Task ClearAsync()
    {
        await storageService.ClearAsync();
    }
}