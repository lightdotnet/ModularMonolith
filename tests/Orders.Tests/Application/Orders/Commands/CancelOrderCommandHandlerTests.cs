using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Inventory.Contracts.Services;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class CancelOrderCommandHandlerTests
{
    private static CancelOrderCommandHandler MakeHandler(OrdersTestHost host, Mock<IInventoryService> inventoryServiceMock) =>
        new(host.Context, inventoryServiceMock.Object, host.DateTime, NullLogger<CancelOrderCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = MakeHandler(host, new Mock<IInventoryService>());

        // Act
        var result = await handler.Handle(
            new CancelOrderCommand(999, new CancelOrderRequest { Reason = "reason" }, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldCancelTheOrder_AndNotRestoreStock_WhenOrderWasStillDraft()
    {
        // Arrange — the order was never placed, so there is no decremented stock to restore.
        using var host = new OrdersTestHost();
        host.DateTime.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tokenBeforeCancel = order.ConcurrencyToken;
        var inventoryServiceMock = new Mock<IInventoryService>();
        var handler = MakeHandler(host, inventoryServiceMock);

        // Act
        var result = await handler.Handle(
            new CancelOrderCommand(order.Id, new CancelOrderRequest { Reason = "customer request" }, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
        Assert.Equal("customer request", reloaded.CancelledReason);
        Assert.Equal(host.DateTime.UtcNow, reloaded.CancelledAt);

        // Status is an Order-owned column, so this change does mark the Order entry itself
        // Modified, and OrdersDbContext.RotateConcurrencyTokens() rotates the token.
        Assert.NotEqual(tokenBeforeCancel, reloaded.ConcurrencyToken);

        inventoryServiceMock.Verify(
            x => x.RestoreForOrderAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCancelTheOrder_AndRestoreStock_WhenOrderWasPlaced()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var placedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var order = OrderBuilder.Placed(placedAt);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inventoryServiceMock = new Mock<IInventoryService>();
        var handler = MakeHandler(host, inventoryServiceMock);

        // Act
        var result = await handler.Handle(
            new CancelOrderCommand(order.Id, new CancelOrderRequest { Reason = "customer request" }, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
        inventoryServiceMock.Verify(
            x => x.RestoreForOrderAsync(order.Id, "user-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStillSucceed_WhenRestoreForOrderThrows()
    {
        // Arrange — restoring stock is best-effort: a failure is only logged, never blocks the cancel.
        using var host = new OrdersTestHost();
        var placedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var order = OrderBuilder.Placed(placedAt);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inventoryServiceMock = new Mock<IInventoryService>();
        inventoryServiceMock
            .Setup(x => x.RestoreForOrderAsync(order.Id, "user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Inventory unavailable"));
        var handler = MakeHandler(host, inventoryServiceMock);

        // Act
        var result = await handler.Handle(
            new CancelOrderCommand(order.Id, new CancelOrderRequest { Reason = "customer request" }, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert — the exception is swallowed (only logged); the cancel itself still succeeded.
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Cancelled, reloaded.Status);
    }
}
