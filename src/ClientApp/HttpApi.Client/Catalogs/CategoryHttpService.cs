using Monolith.Catalog;

namespace Monolith.HttpApi.Catalogs;

public class CategoryHttpService(IHttpClientFactory httpClientFactory) : TryHttpClient(httpClientFactory)
{
    public const string BasePath = "category";

    public Task<Result<IEnumerable<CategoryVm>>> GetAsync()
    {
        var url = BasePath;

        return TryGetAsync<IEnumerable<CategoryVm>>(url);
    }

    public Task<Result> CreateAsync(CreateCategoryRequest request)
    {
        var url = BasePath;

        return TryPostAsync(url, request);
    }

    public Task<Result> UpdateAsync(CategoryDto request)
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
