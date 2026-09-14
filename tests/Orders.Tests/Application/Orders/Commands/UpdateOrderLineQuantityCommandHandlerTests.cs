using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class UpdateOrderLineQuantityCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new UpdateOrderLineQuantityCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateOrderLineQuantityCommand(999, 999, new UpdateOrderLineQuantityRequest { Quantity = 2 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldUpdateQuantity()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine(quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lineId = order.Lines[0].Id;
        var handler = new UpdateOrderLineQuantityCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new UpdateOrderLineQuantityCommand(order.Id, lineId, new UpdateOrderLineQuantityRequest { Quantity = 5 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(5, reloaded.Lines[0].Quantity);

        // Note: OrdersDbContext.RotateConcurrencyTokens() only inspects
        // ChangeTracker.Entries<Order>() — updating a child OrderLine's own scalar property does
        // not, by itself, mark the parent Order entry as Modified, so Order.ConcurrencyToken is
        // *not* rotated by this call. See CancelOrderCommandHandlerTests for the corresponding
        // positive case (an actual Order-owned column changing does rotate the token). Flagged to
        // the requester as a discovered gap — not fixed here.
    }
}
