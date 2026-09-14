using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class RemoveOrderLineCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new RemoveOrderLineCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveOrderLineCommand(999, 999),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldRemoveTheLine()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine();
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var lineId = order.Lines[0].Id;
        var handler = new RemoveOrderLineCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RemoveOrderLineCommand(order.Id, lineId),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Empty(reloaded.Lines);
    }
}
