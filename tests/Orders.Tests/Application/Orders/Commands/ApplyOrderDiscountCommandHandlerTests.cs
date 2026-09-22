using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

public class ApplyOrderDiscountCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new ApplyOrderDiscountCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new ApplyOrderDiscountCommand(
                999,
                new ApplyOrderDiscountRequest { Kind = OrderDiscountKind.FixedAmount, Value = 10m }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldApplyTheDiscount()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ApplyOrderDiscountCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new ApplyOrderDiscountCommand(
                order.Id,
                new ApplyOrderDiscountRequest { Kind = OrderDiscountKind.FixedAmount, Value = 30m }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderDiscountKind.FixedAmount, reloaded.Discount?.Kind);
        Assert.Equal(30m, reloaded.Discount?.Value);
    }

    [Fact]
    public async Task Handle_ShouldMutateAnExistingDiscountInPlace_AndPersistTheNewValues_WhenCalledTwice()
    {
        // Arrange — regression coverage for OrderDiscount.Update's owned-type persistence, round
        // tripped through a real Sqlite SaveChangesAsync, not just an in-memory reference check.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ApplyOrderDiscountCommandHandler(host.Context);
        await handler.Handle(
            new ApplyOrderDiscountCommand(
                order.Id,
                new ApplyOrderDiscountRequest { Kind = OrderDiscountKind.FixedAmount, Value = 30m }),
            TestContext.Current.CancellationToken);

        // Act — apply again with a different kind/value on the same, already-persisted discount.
        var result = await handler.Handle(
            new ApplyOrderDiscountCommand(
                order.Id,
                new ApplyOrderDiscountRequest { Kind = OrderDiscountKind.Percentage, Value = 25m }),
            TestContext.Current.CancellationToken);

        // Assert — AsNoTracking forces a fresh round trip through Sqlite, ruling out any
        // in-memory-only artifact from the already-tracked instance.
        Assert.True(result.IsSuccess);
        var reloaded = await host.Context.Orders
            .AsNoTracking()
            .FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderDiscountKind.Percentage, reloaded.Discount?.Kind);
        Assert.Equal(25m, reloaded.Discount?.Value);
    }
}
