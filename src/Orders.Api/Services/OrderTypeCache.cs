using Light.Extensions.Caching;
using StarterKit.Orders.Api.Data;
using StarterKit.Orders.Api.Domain.OrderTypes;

namespace StarterKit.Orders.Api.Services;

internal sealed class OrderTypeCache(
    ICacheService cacheService,
    OrdersDbContext context) : IOrderTypeCache
{
    private const string CacheKey = "orders:order-types:all";

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var all = await context.OrderTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(CacheKey, all, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<OrderType>> GetAllAsync(CancellationToken cancellationToken)
    {
        var cached = await cacheService.GetAsync<List<OrderType>>(CacheKey, cancellationToken);

        if (cached is { Count: > 0 })
            return cached;

        await ReloadAsync(cancellationToken);

        cached = await cacheService.GetAsync<List<OrderType>>(CacheKey, cancellationToken);

        return cached ?? [];
    }

    public async Task<OrderType?> GetAsync(string id, OrderTypeCategory category, CancellationToken cancellationToken)
    {
        var all = await GetAllAsync(cancellationToken);

        return all.FirstOrDefault(x => x.Id == id && x.Category == category);
    }
}
