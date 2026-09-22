using Light.Extensions.Caching;

namespace Framework.Tests.Persistence.TestSupport;

/// <summary>
///     Hand-written in-memory fake for <see cref="ICacheService"/>. This test project has no Moq
///     reference — matching its established "hand-written fakes" style (see
///     <c>FakeCurrentUser</c>/<c>FakeDateTime</c> used elsewhere in this solution's test projects).
/// </summary>
/// <remarks>
///     Values are boxed as <see cref="object"/> per key so the generic <c>Get</c>/<c>TryGet</c>
///     members can cast back to the requested type. <see cref="ThrowOnGet"/>/<see cref="ThrowOnSet"/>/
///     <see cref="ThrowOnRemove"/> let tests simulate a cache-backend failure, mirroring the real
///     <c>MemoryCacheService</c>'s swallow semantics: the non-<c>Try</c> members let a configured
///     failure propagate, and the <c>Try</c> members catch it and fall back to a no-op/default.
/// </remarks>
public sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _store = [];

    public bool ThrowOnGet { get; set; }

    public bool ThrowOnSet { get; set; }

    public bool ThrowOnRemove { get; set; }

    public int GetAsyncCallCount { get; private set; }

    public int TryGetAsyncCallCount { get; private set; }

    public int SetAsyncCallCount { get; private set; }

    public int TrySetAsyncCallCount { get; private set; }

    public int RemoveAsyncCallCount { get; private set; }

    /// <summary>Test introspection: whether <paramref name="key"/> currently has a stored value.</summary>
    public bool ContainsKey(string key) => _store.ContainsKey(key);

    /// <summary>Test-only helper: seed the fake cache directly, bypassing Set/TrySet call tracking.</summary>
    public void Seed<T>(string key, T value) => _store[key] = value;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        GetAsyncCallCount++;

        if (ThrowOnGet)
            throw new InvalidOperationException("Simulated cache GET failure.");

        return Task.FromResult(_store.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public async Task<T?> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        TryGetAsyncCallCount++;

        try
        {
            return await GetAsync<T>(key, cancellationToken);
        }
        catch (Exception)
        {
            return default;
        }
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        SetAsyncCallCount++;

        if (ThrowOnSet)
            throw new InvalidOperationException("Simulated cache SET failure.");

        _store[key] = value;
        return Task.CompletedTask;
    }

    public async Task TrySetAsync<T>(
        string key,
        T value,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        TrySetAsyncCallCount++;

        try
        {
            await SetAsync(key, value, slidingExpiration, cancellationToken);
        }
        catch (Exception)
        {
            // Swallow, mirrors MemoryCacheService.TrySetAsync.
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemoveAsyncCallCount++;

        if (ThrowOnRemove)
            throw new InvalidOperationException("Simulated cache REMOVE failure.");

        _store.Remove(key);
        return Task.CompletedTask;
    }

    // Sync members required by ICacheService but not exercised by CacheRepositoryBase<T> (which is
    // async-only) — implemented as thin wrappers over the async members for consistency.
    public T? Get<T>(string key) => GetAsync<T>(key).GetAwaiter().GetResult();

    public T? TryGet<T>(string key) => TryGetAsync<T>(key).GetAwaiter().GetResult();

    public void Set<T>(string key, T value, TimeSpan? slidingExpiration = null) =>
        SetAsync(key, value, slidingExpiration).GetAwaiter().GetResult();

    public void TrySet<T>(string key, T value, TimeSpan? slidingExpiration = null) =>
        TrySetAsync(key, value, slidingExpiration).GetAwaiter().GetResult();

    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();
}
