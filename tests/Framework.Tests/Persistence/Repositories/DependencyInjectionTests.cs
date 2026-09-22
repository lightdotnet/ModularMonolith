using Framework.Tests.Persistence.TestSupport;
using Light.Extensions.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.Persistence.Repositories;
using Xunit;

namespace Framework.Tests.Persistence.Repositories;

/// <summary>
///     Exercises <see cref="DependencyInjection.AddCachingServices"/>. Its signature takes no
///     <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> and does not call
///     <c>AddCache</c> itself - callers must register a cache provider (e.g. <c>AddAppCache</c>)
///     separately before calling this.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddCachingServices_ShouldRegisterCacheRepository_AsScopedOpenGeneric()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCachingServices();

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ICacheRepository<,>));
        Assert.Equal(typeof(CacheRepository<,>), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddCachingServices_ShouldResolve_ConcreteCacheRepository_ForRegisteredContextAndEntity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<ICacheService>(new FakeCacheService());
        services.AddLogging();
        services.AddCachingServices();

        // Act
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICacheRepository<TestAggregate, TestDbContext>>();

        // Assert
        Assert.IsType<CacheRepository<TestAggregate, TestDbContext>>(repository);
    }
}
