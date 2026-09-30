using Framework.Tests.EventBusMassTransitRabbitMQ.TestSupport;
using MassTransit;
using Xunit;

namespace Framework.Tests.EventBusMassTransitRabbitMQ;

public class AppConsumerDefinitionTests
{
    private static readonly IEndpointNameFormatter Formatter = KebabCaseEndpointNameFormatter.Instance;

    [Fact]
    public void Constructor_ShouldSetConcurrentMessageLimitTo10()
    {
        // Act
        var definition = new TestBoundEventConsumerDefinition();

        // Assert
        Assert.Equal(10, definition.ConcurrentMessageLimit);
    }

    [Fact]
    public void Constructor_WithPrefix_ShouldSetConcurrentMessageLimitTo10()
    {
        // Act
        var definition = new PrefixedTestBoundEventConsumerDefinition();

        // Assert
        Assert.Equal(10, definition.ConcurrentMessageLimit);
    }

    [Fact]
    public void GetEndpointName_WithoutPrefix_ShouldBeTheBindingName()
    {
        // Arrange
        IConsumerDefinition definition = new TestBoundEventConsumerDefinition();

        // Act
        var endpointName = definition.GetEndpointName(Formatter);

        // Assert
        Assert.Equal("test-bound-event", endpointName);
    }

    [Fact]
    public void GetEndpointName_WithPrefix_ShouldBePrefixedBindingName()
    {
        // Arrange
        IConsumerDefinition definition = new PrefixedTestBoundEventConsumerDefinition();

        // Act
        var endpointName = definition.GetEndpointName(Formatter);

        // Assert
        Assert.Equal("billing-test-bound-event", endpointName);
    }

    [Fact]
    public void GetEndpointName_WithPrefixButNoBindingName_ShouldFallBackToFormatterName()
    {
        // Arrange
        IConsumerDefinition definition = new PrefixedTestUnboundEventConsumerDefinition();

        // Act
        var endpointName = definition.GetEndpointName(Formatter);

        // Assert
        // the vendor base ignores the prefix when the event has no [BindingName]
        Assert.Equal(Formatter.Consumer<TestUnboundEventConsumer>(), endpointName);
        Assert.DoesNotContain("billing", endpointName);
    }
}
