using Light.EventBus.Abstractions;
using Moq;
using StarterKit.Modules.Identity.Application.Common;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Shared;
using Xunit;

namespace Identity.Tests.Application.Common;

public class IntegrationEventCollectorTests
{
    [Fact]
    public void HasEvents_ShouldBeFalse_WhenNothingWasAdded()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Assert.False(collector.HasEvents);
    }

    [Fact]
    public void Add_ShouldThrow_WhenEventIsNull()
    {
        // Arrange
        var collector = new IntegrationEventCollector();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collector.Add<UserDeletedIntegrationEvent>(null!));
        Assert.False(collector.HasEvents);
    }

    [Fact]
    public async Task PublishAllAsync_ShouldPublishEachEventByItsConcreteType_InOrder()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var deleted = new UserDeletedIntegrationEvent("user-1", 1);
        var statusChanged = new UserStatusChangedIntegrationEvent("user-2", false, 2);
        var published = new List<object>();
        var bus = new Mock<IEventBus>();
        bus
            .Setup(b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<UserDeletedIntegrationEvent, CancellationToken>((e, _) => published.Add(e))
            .Returns(Task.CompletedTask);
        bus
            .Setup(b => b.Publish(It.IsAny<UserStatusChangedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<UserStatusChangedIntegrationEvent, CancellationToken>((e, _) => published.Add(e))
            .Returns(Task.CompletedTask);

        collector.Add(deleted);
        collector.Add(statusChanged);

        // Act
        await collector.PublishAllAsync(bus.Object, TestContext.Current.CancellationToken);

        // Assert: each publish was routed through the concrete generic instantiation, never the base type.
        // Moq matches a Publish<IntegrationEvent> verification against derived instantiations too,
        // so inspect the recorded generic type arguments directly.
        Assert.Equal([deleted, statusChanged], published);
        Assert.Equal(
            [typeof(UserDeletedIntegrationEvent), typeof(UserStatusChangedIntegrationEvent)],
            bus.Invocations.Select(i => i.Method.GetGenericArguments().Single()));
    }

    [Fact]
    public async Task PublishAllAsync_ShouldPassCancellationTokenThrough()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        using var cts = new CancellationTokenSource();
        var bus = new Mock<IEventBus>();
        collector.Add(new UserDeletedIntegrationEvent("user-1", 1));

        // Act
        await collector.PublishAllAsync(bus.Object, cts.Token);

        // Assert
        bus.Verify(b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task PublishAllAsync_ShouldNotClearTheBuffer()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var bus = new Mock<IEventBus>();
        collector.Add(new UserDeletedIntegrationEvent("user-1", 1));

        // Act
        await collector.PublishAllAsync(bus.Object, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(collector.HasEvents);
    }

    [Fact]
    public async Task Clear_ShouldDropBufferedEvents()
    {
        // Arrange
        var collector = new IntegrationEventCollector();
        var bus = new Mock<IEventBus>();
        collector.Add(new UserDeletedIntegrationEvent("user-1", 1));

        // Act
        collector.Clear();
        await collector.PublishAllAsync(bus.Object, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(collector.HasEvents);
        bus.Verify(
            b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
