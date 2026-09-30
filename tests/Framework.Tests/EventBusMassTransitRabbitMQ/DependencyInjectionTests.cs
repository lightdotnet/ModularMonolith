using Framework.Tests.EventBusMassTransitRabbitMQ.TestSupport;
using Light.EventBus.Abstractions;
using Light.MassTransit.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StarterKit.EventBusMassTransitRabbitMQ;
using Xunit;

namespace Framework.Tests.EventBusMassTransitRabbitMQ;

public class DependencyInjectionTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> EnabledSettings() => new()
    {
        ["RabbitMQ:Enable"] = "true",
        ["RabbitMQ:Host"] = "localhost",
        ["RabbitMQ:Username"] = "guest",
        ["RabbitMQ:Password"] = "guest",
    };

    private static ServiceDescriptor GetSingleEventBusDescriptor(IServiceCollection services) =>
        Assert.Single(services, x => x.ServiceType == typeof(IEventBus));

    [Fact]
    public void AddEventBus_ShouldRegisterNoOpEventBusAsSingleton_WhenRabbitMQSectionIsMissing()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(BuildConfiguration([]));

        // Assert
        var descriptor = GetSingleEventBusDescriptor(services);
        Assert.Equal(typeof(NoOpEventBus), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddEventBus_ShouldRegisterNoOpEventBusAsSingleton_WhenDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        var values = EnabledSettings();
        values["RabbitMQ:Enable"] = "false";

        // Act
        services.AddEventBus(BuildConfiguration(values));

        // Assert
        var descriptor = GetSingleEventBusDescriptor(services);
        Assert.Equal(typeof(NoOpEventBus), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddEventBus_ShouldResolveNoOpEventBus_WhenDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEventBus(BuildConfiguration([]));
        using var provider = services.BuildServiceProvider();

        // Act
        var bus = provider.GetRequiredService<IEventBus>();

        // Assert
        Assert.IsType<NoOpEventBus>(bus);
    }

    [Fact]
    public void AddEventBus_ShouldRegisterRabbitMQEventBus_WhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(BuildConfiguration(EnabledSettings()));

        // Assert
        // only descriptors are inspected: no provider is built, so no bus is started
        var descriptor = GetSingleEventBusDescriptor(services);
        Assert.NotEqual(typeof(NoOpEventBus), descriptor.ImplementationType);
        Assert.Equal(typeof(RabbitMQEventBus), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddEventBus_ShouldRegisterConsumersFromModuleConsumers_WhenEnabledWithAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(BuildConfiguration(EnabledSettings()), typeof(TestModuleConsumer).Assembly);

        // Assert
        // TestModuleConsumer (an AppModuleConsumer) is found by assembly scanning and registers its consumer
        Assert.Contains(services, x => x.ServiceType == typeof(TestBoundEventConsumer));
    }

    [Theory]
    [InlineData("RabbitMQ:Host")]
    [InlineData("RabbitMQ:Username")]
    [InlineData("RabbitMQ:Password")]
    public void AddEventBus_ShouldThrowArgumentException_WhenEnabledAndRequiredSettingIsMissing(string key)
    {
        // Arrange
        var services = new ServiceCollection();
        var values = EnabledSettings();
        values.Remove(key);
        var configuration = BuildConfiguration(values);

        // Act
        var exception = Assert.Throws<ArgumentException>(() => services.AddEventBus(configuration));

        // Assert
        // AddRabbitMQEventBus validates eagerly at registration time, before anything is registered
        Assert.Contains(key.Split(':')[1], exception.Message);
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IEventBus));
    }
}
