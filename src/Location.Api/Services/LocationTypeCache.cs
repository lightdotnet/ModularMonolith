using Light.Extensions.Caching;
using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.LocationTypes;

namespace StarterKit.Locations.Api.Services;

internal sealed class LocationTypeCache(
    ICacheService cacheService,
    LocationDbContext context) : ILocationTypeCache
{
    private const string CacheKey = "location:location-types:all";

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var all = await context.LocationTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(CacheKey, all, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LocationType>> GetAllAsync(CancellationToken cancellationToken)
    {
        var cached = await cacheService.GetAsync<List<LocationType>>(CacheKey, cancellationToken);

        if (cached is { Count: > 0 })
            return cached;

        await ReloadAsync(cancellationToken);

        cached = await cacheService.GetAsync<List<LocationType>>(CacheKey, cancellationToken);

        return cached ?? [];
    }

    public async Task<LocationType?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var all = await GetAllAsync(cancellationToken);

        return all.FirstOrDefault(x => x.Id == id);
    }
}
