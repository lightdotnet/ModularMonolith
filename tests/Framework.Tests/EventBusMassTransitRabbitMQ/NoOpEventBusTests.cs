using Framework.Tests.EventBusMassTransitRabbitMQ.TestSupport;
using Microsoft.Extensions.Logging;
using Moq;
using StarterKit.EventBusMassTransitRabbitMQ;
using Xunit;

namespace Framework.Tests.EventBusMassTransitRabbitMQ;

public class NoOpEventBusTests
{
    [Fact]
    public async Task Publish_ShouldCompleteAndLogOnceAtInformationWithEventNameAndId()
    {
        // Arrange
        var logger = new Mock<ILogger<NoOpEventBus>>();
        var bus = new NoOpEventBus(logger.Object);
        var message = new TestBoundEvent();

        // Act
        var task = bus.Publish(message, TestContext.Current.CancellationToken);
        await task;

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains(nameof(TestBoundEvent)) && v.ToString()!.Contains(message.Id)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // nothing else is logged
        logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
