using Microsoft.EntityFrameworkCore;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Payments.Commands;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Payments;
using StarterKit.Shared.Constants;
using Xunit;

namespace Orders.Tests.Application.Payments.Commands;

public class RecordPaymentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var handler = new RecordPaymentCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                999,
                new RecordPaymentRequest
                {
                    Amount = 50m,
                    Currency = CurrencyConstants.Default,
                    Method = PaymentMethod.Cash,
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldRecordThePayment_AndReconcileToPartiallyPaid()
    {
        // Arrange — Total is 100 (one line, unit price 100, quantity 1).
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RecordPaymentCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 40m,
                    Currency = CurrencyConstants.Default,
                    Method = PaymentMethod.Cash,
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloadedOrder = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.PartiallyPaid, reloadedOrder.Status);
        Assert.Equal(40m, reloadedOrder.AmountPaid.Amount);
        var payment = await host.Context.Payments.FirstAsync(x => x.Id == result.Data, TestContext.Current.CancellationToken);
        Assert.Equal(40m, payment.Amount.Amount);
        Assert.Equal(order.OrderCode.Value, payment.OrderCode);
    }

    [Fact]
    public async Task Handle_ShouldReconcileToPaid_WhenTheTotalIsCoveredAcrossMultiplePayments()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new RecordPaymentCommandHandler(host.Context);
        var firstPayment = new RecordPaymentRequest
        {
            Amount = 60m,
            Currency = CurrencyConstants.Default,
            Method = PaymentMethod.Cash,
            PaidAt = host.DateTime.UtcNow,
        };
        await handler.Handle(new RecordPaymentCommand(order.Id, firstPayment, "user-1"), TestContext.Current.CancellationToken);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                firstPayment with { Amount = 40m },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var reloadedOrder = await host.Context.Orders.FirstAsync(x => x.Id == order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderStatus.Paid, reloadedOrder.Status);
        Assert.Equal(100m, reloadedOrder.AmountPaid.Amount);
    }
}
