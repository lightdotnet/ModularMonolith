using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class RemoveOrderDiscountCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new RemoveOrderDiscountCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveOrderDiscountCommand(999),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldRemoveTheDiscount()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        order.ApplyDiscount(OrderDiscountKind.FixedAmount, 10m);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RemoveOrderDiscountCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveOrderDiscountCommand(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Null(reloaded.Discount);
    }
}
