using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Payments.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Payments;
using Xunit;

namespace Orders.Tests.Application.Payments.Commands;

public class VoidPaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new VoidPaymentCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new VoidPaymentCommand(999, new VoidPaymentRequest { Reason = "reason" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldVoidThePayment_AndBringTheOrderBackDownFromPaidToPlaced()
    {
        // Arrange — the only payment on this order is the one being voided, so the order should
        // fall all the way back to Placed, not stay Paid because of a stale IsVoided read.
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PaymentBuilder.Build(order.Id, 100m, host.DateTime.UtcNow);
        await host.Context.Payments.AddAsync(payment, TestContext.Current.CancellationToken);
        order.ReconcilePaymentStatus(100m);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new VoidPaymentCommandHandler(host.Context, host.DateTime);

        // Act
        var result = await handler.Handle(
            new VoidPaymentCommand(payment.Id, new VoidPaymentRequest { Reason = "refund issued" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloadedPayment = await host.Context.Payments.FirstAsync(x => x.Id == payment.Id, TestContext.Current.CancellationToken);
        Assert.True(reloadedPayment.IsVoided);
        Assert.Equal("refund issued", reloadedPayment.VoidReason);
        var reloadedOrder = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Placed, reloadedOrder.Status);
        Assert.Equal(0m, reloadedOrder.AmountPaid.Amount);
    }
}
