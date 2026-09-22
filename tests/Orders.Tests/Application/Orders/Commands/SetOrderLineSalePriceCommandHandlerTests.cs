using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class SetOrderLineSalePriceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new SetOrderLineSalePriceCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SetOrderLineSalePriceCommand(999, 999, new SetOrderLineSalePriceRequest { SalePrice = 50m }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldSetSalePrice_OnTheTargetLine()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lineId = order.Lines[0].Id;
        var handler = new SetOrderLineSalePriceCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SetOrderLineSalePriceCommand(order.Id, lineId, new SetOrderLineSalePriceRequest { SalePrice = 75m }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(75m, reloaded.Lines[0].RequestedSalePrice?.Amount);
    }
}
