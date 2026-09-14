using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class AddOrderFeeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new AddOrderFeeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                999,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 10m, Type = OrderFeeType.Shipping }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldAddTheFee()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Draft();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new AddOrderFeeCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                order.Id,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 15m, Type = OrderFeeType.Shipping }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Fees)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        var fee = Assert.Single(reloaded.Fees);
        Assert.Equal("Shipping", fee.Name);
        Assert.Equal(15m, fee.Amount.Amount);
        Assert.Equal(OrderFeeType.Shipping, fee.Type);
    }
}
