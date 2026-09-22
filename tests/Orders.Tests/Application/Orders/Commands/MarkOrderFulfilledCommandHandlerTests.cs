using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class MarkOrderFulfilledCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new MarkOrderFulfilledCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new MarkOrderFulfilledCommand(999),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_WhenOrderIsNotPaid()
    {
        // Arrange — Order.MarkFulfilled's own guard throws directly; this handler has no
        // try/catch translation, so the domain exception propagates as-is.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new MarkOrderFulfilledCommandHandler(host.Context, host.DateTime);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new MarkOrderFulfilledCommand(order.Id),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldMarkFulfilled_WhenOrderIsPaid()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Paid(host.DateTime.UtcNow);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new MarkOrderFulfilledCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new MarkOrderFulfilledCommand(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Fulfilled, reloaded.Status);
    }
}
