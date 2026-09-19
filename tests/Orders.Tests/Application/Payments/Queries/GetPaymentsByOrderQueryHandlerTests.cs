using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Payments.Queries;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Application.Payments.Queries;

public class GetPaymentsByOrderQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnOnlyPaymentsForTheGivenOrder_NewestFirst()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("CASH", OrderTypeCategory.Payment, "Cash"),
            TestContext.Current.CancellationToken);
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 200m, quantity: 1);
        var otherOrder = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 200m, quantity: 1);
        await host.Context.Orders.AddRangeAsync([order, otherOrder], TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Created (the ordering column) is stamped from the fake clock, not the system clock, so it
        // must be advanced by hand between rows to get a deterministic newest-first order.
        host.DateTime.UtcNow = host.DateTime.UtcNow.AddMinutes(1);
        var firstPayment = PaymentBuilder.Build(order.Id, 50m, host.DateTime.UtcNow);
        await host.Context.Payments.AddAsync(firstPayment, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        host.DateTime.UtcNow = host.DateTime.UtcNow.AddMinutes(1);
        var secondPayment = PaymentBuilder.Build(order.Id, 50m, host.DateTime.UtcNow);
        await host.Context.Payments.AddAsync(secondPayment, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        host.DateTime.UtcNow = host.DateTime.UtcNow.AddMinutes(1);
        var paymentOnOtherOrder = PaymentBuilder.Build(otherOrder.Id, 200m, host.DateTime.UtcNow);
        await host.Context.Payments.AddAsync(paymentOnOtherOrder, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPaymentsByOrderQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetPaymentsByOrderQuery(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(secondPayment.Id, result[0].Id);
        Assert.Equal(firstPayment.Id, result[1].Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnTheOrderCodeSnapshot_OnEachPayment()
    {
        // Arrange
        using var host = new OrdersTestHost();
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("CASH", OrderTypeCategory.Payment, "Cash"),
            TestContext.Current.CancellationToken);
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 200m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PaymentBuilder.Build(order.Id, 50m, host.DateTime.UtcNow, orderCode: order.OrderCode.Value);
        await host.Context.Payments.AddAsync(payment, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPaymentsByOrderQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new GetPaymentsByOrderQuery(order.Id),
            TestContext.Current.CancellationToken);

        // Assert
        var dto = Assert.Single(result);
        Assert.Equal(order.OrderCode.Value, dto.OrderCode);
        Assert.Equal("CASH", dto.PaymentTypeId);
        Assert.Equal("Cash", dto.PaymentTypeName);
    }
}
