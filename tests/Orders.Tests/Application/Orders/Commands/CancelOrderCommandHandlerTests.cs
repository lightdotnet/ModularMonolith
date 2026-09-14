using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class CancelOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new CancelOrderCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new CancelOrderCommand(999, new CancelOrderRequest { Reason = "reason" }, "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldCancelTheOrder()
    {
        // Arrange
        using var host = new OrdersTestHost();
        host.DateTime.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var tokenBeforeCancel = order.ConcurrencyToken;
        var handler = new CancelOrderCommandHandler(host.Context, host.DateTime);

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
    }
}
