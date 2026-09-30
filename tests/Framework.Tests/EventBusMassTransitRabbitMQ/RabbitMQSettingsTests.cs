using Microsoft.Extensions.Configuration;
using StarterKit.EventBusMassTransitRabbitMQ;
using Xunit;

namespace Framework.Tests.EventBusMassTransitRabbitMQ;

public class RabbitMQSettingsTests
{
    [Fact]
    public void Bind_ShouldReadAllValuesFromRabbitMQSection()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:Enable"] = "true",
                ["RabbitMQ:Host"] = "rabbitmq://localhost",
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "secret",
            })
            .Build();

        // Act
        var settings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();

        // Assert
        Assert.NotNull(settings);
        Assert.True(settings.Enable);
        Assert.Equal("rabbitmq://localhost", settings.Host);
        Assert.Equal("guest", settings.Username);
        Assert.Equal("secret", settings.Password);
    }

    [Fact]
    public void Bind_ShouldDefaultEnableToFalse_WhenNotConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["RabbitMQ:Host"] = "localhost" })
            .Build();

        // Act
        var settings = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();

        // Assert
        Assert.NotNull(settings);
        Assert.False(settings.Enable);
    }
}
