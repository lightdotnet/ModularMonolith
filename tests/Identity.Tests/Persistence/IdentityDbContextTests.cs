using Identity.Tests.TestSupport;
using Light.EventBus.Abstractions;
using Light.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StarterKit.Modules.Identity.Contracts.IntegrationEvents;
using StarterKit.Modules.Identity.Domain;
using StarterKit.Modules.Identity.IntegrationEvents;
using StarterKit.Modules.Identity.Persistence;
using Xunit;

namespace Identity.Tests.Persistence;

public class IdentityDbContextTests
{
    private sealed class Sut(
        IdentityDbContext context,
        IntegrationEventCollector integrationEvents,
        Mock<IEventBus> eventBus)
        : IDisposable
    {
        public IdentityDbContext Context { get; } = context;

        public IntegrationEventCollector IntegrationEvents { get; } = integrationEvents;

        public Mock<IEventBus> EventBus { get; } = eventBus;

        public void Dispose() => Context.Dispose();
    }

    /// <summary>
    /// Fails every save inside <c>base.SaveChangesAsync</c>, i.e. after the context's own
    /// pre-save steps, which is where a real database failure would surface.
    /// </summary>
    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated save failure.");
    }

    private static Sut CreateSut(bool failSaves = false)
    {
        var builder = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());

        if (failSaves)
            builder.AddInterceptors(new FailingSaveInterceptor());

        var integrationEvents = new IntegrationEventCollector();
        var eventBus = new Mock<IEventBus>();

        var context = new IdentityDbContext(
            new FakeCurrentUser { UserId = "admin" },
            new FakeDateTime(),
            Mock.Of<IPublisher>(),
            integrationEvents,
            eventBus.Object,
            NullLogger<IdentityDbContext>.Instance,
            builder.Options);

        return new Sut(context, integrationEvents, eventBus);
    }

    private static UserDeletedIntegrationEvent SampleEvent() => new("user-1", 1);

    [Fact]
    public async Task SaveChangesAsync_ShouldPublishEvents_OnlyAfterTheSaveCommitted()
    {
        // Arrange
        using var sut = CreateSut();
        var user = new User { UserName = "jane.doe" };
        sut.Context.Users.Add(user);
        sut.IntegrationEvents.Add(SampleEvent());

        EntityState? stateWhenPublished = null;
        sut.EventBus
            .Setup(b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => stateWhenPublished = sut.Context.Entry(user).State)
            .Returns(Task.CompletedTask);

        // Act
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert: the entry was already accepted (Unchanged) when the event went out.
        Assert.Equal(EntityState.Unchanged, stateWhenPublished);
        sut.EventBus.Verify(
            b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.False(sut.IntegrationEvents.HasEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotPublish_AndClearCollector_WhenSaveFails()
    {
        // Arrange
        using var sut = CreateSut(failSaves: true);
        sut.Context.Users.Add(new User { UserName = "jane.doe" });
        sut.IntegrationEvents.Add(SampleEvent());

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.False(sut.IntegrationEvents.HasEvents);
        sut.EventBus.Verify(
            b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSwallowPublishFailure_AndClearCollector()
    {
        // Arrange: no outbox - a publish failure after the commit is logged, not rethrown.
        using var sut = CreateSut();
        sut.Context.Users.Add(new User { UserName = "jane.doe" });
        sut.IntegrationEvents.Add(SampleEvent());
        sut.EventBus
            .Setup(b => b.Publish(It.IsAny<UserDeletedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker unavailable."));

        // Act
        var written = await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(written > 0);
        Assert.False(sut.IntegrationEvents.HasEvents);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotTouchTheBus_WhenNoEventsArePending()
    {
        // Arrange
        using var sut = CreateSut();
        sut.Context.Users.Add(new User { UserName = "jane.doe" });

        // Act
        await sut.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(sut.EventBus.Invocations);
    }

    [Fact]
    public void SaveChanges_ShouldThrowNotSupported_WhenEventsArePending()
    {
        // Arrange
        using var sut = CreateSut();
        var user = new User { UserName = "jane.doe" };
        sut.Context.Users.Add(user);
        sut.IntegrationEvents.Add(SampleEvent());

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => sut.Context.SaveChanges());
        Assert.Equal(EntityState.Added, sut.Context.Entry(user).State);
        Assert.True(sut.IntegrationEvents.HasEvents);
        Assert.Empty(sut.EventBus.Invocations);
    }

    [Fact]
    public void SaveChanges_ShouldSave_WhenNoEventsArePending()
    {
        // Arrange
        using var sut = CreateSut();
        var user = new User { UserName = "jane.doe" };
        sut.Context.Users.Add(user);

        // Act
        var written = sut.Context.SaveChanges();

        // Assert
        Assert.True(written > 0);
        Assert.Equal(EntityState.Unchanged, sut.Context.Entry(user).State);
    }
}
