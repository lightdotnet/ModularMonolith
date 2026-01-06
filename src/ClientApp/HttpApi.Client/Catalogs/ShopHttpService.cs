using Monolith.Catalog;

namespace Monolith.HttpApi.Catalogs;

public class ShopHttpService(IHttpClientFactory httpClientFactory)
    : TryHttpClient(httpClientFactory)
{
    public const string BasePath = "shop";

    public Task<PagedResult<ShopDto>> SearchAsync(ShopLookup lookup)
    {
        var url = $"{BasePath}/search";

        return TryGetPagedAsync<ShopDto>(url, lookup);
    }

    public Task<Result> CreateAsync(CreateShopRequest request)
    {
        var url = BasePath;

        return TryPostAsync(url, request);
    }

    public Task<Result> UpdateAsync(ShopDto request)
    {
        var url = BasePath;

        return TryPutAsync(url, request);
    }

    public Task<Result> DeleteAsync(string id)
    {
        var url = $"{BasePath}/{id}";

        return TryDeleteAsync(url);
    }
}
