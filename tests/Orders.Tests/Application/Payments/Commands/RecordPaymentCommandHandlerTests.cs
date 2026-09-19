using Light.Extensions.Caching;
using Microsoft.EntityFrameworkCore;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Application.Payments.Commands;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
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
        var cacheMock = await CacheReturningAsync(host, "CASH");
        var handler = new RecordPaymentCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                999,
                new RecordPaymentRequest
                {
                    Amount = 50m,
                    Currency = CurrencyConstants.Default,
                    PaymentTypeId = "CASH",
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPaymentTypeDoesNotExist()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = new Mock<IOrderTypeCache>();
        cacheMock
            .Setup(x => x.GetAsync("MISSING", OrderTypeCategory.Payment, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderType?)null);
        var handler = new RecordPaymentCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 50m,
                    Currency = CurrencyConstants.Default,
                    PaymentTypeId = "MISSING",
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPaymentTypeIsInactive()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inactiveType = OrderType.Create("CASH", OrderTypeCategory.Payment, "Cash");
        inactiveType.Update("Cash", OrderTypeStatus.Inactive);
        await host.Context.OrderTypes.AddAsync(inactiveType, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = new Mock<IOrderTypeCache>();
        cacheMock
            .Setup(x => x.GetAsync("CASH", OrderTypeCategory.Payment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveType);
        var handler = new RecordPaymentCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 50m,
                    Currency = CurrencyConstants.Default,
                    PaymentTypeId = "CASH",
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// A Fee-category type must not be usable as a payment type. Uses the real <see cref="OrderTypeCache"/>
    /// (not a mock) seeded with an "OTHER" row under <see cref="OrderTypeCategory.Fee"/> only, so this
    /// exercises the actual composite-key guard end to end: the handler looks the id up under
    /// <see cref="OrderTypeCategory.Payment"/>, which genuinely finds nothing.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenIdBelongsToAFeeCategoryType()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await host.Context.OrderTypes.AddAsync(
            OrderType.Create("OTHER", OrderTypeCategory.Fee, "Other"),
            TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cache = new OrderTypeCache(CreateStatefulCacheMock(), host.Context);
        var handler = new RecordPaymentCommandHandler(host.Context, cache);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 50m,
                    Currency = CurrencyConstants.Default,
                    PaymentTypeId = "OTHER",
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
        var cacheMock = await CacheReturningAsync(host, "CASH");
        var handler = new RecordPaymentCommandHandler(host.Context, cacheMock.Object);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 40m,
                    Currency = CurrencyConstants.Default,
                    PaymentTypeId = "CASH",
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
        Assert.Equal("CASH", payment.PaymentTypeId);
        Assert.Equal("CASH", payment.PaymentTypeName);
    }

    [Fact]
    public async Task Handle_ShouldReconcileToPaid_WhenTheTotalIsCoveredAcrossMultiplePayments()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m, quantity: 1);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cacheMock = await CacheReturningAsync(host, "CASH");
        var handler = new RecordPaymentCommandHandler(host.Context, cacheMock.Object);
        var firstPayment = new RecordPaymentRequest
        {
            Amount = 60m,
            Currency = CurrencyConstants.Default,
            PaymentTypeId = "CASH",
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

    /// <summary>
    /// Seeds a real <see cref="OrderType"/> row (the FK the persisted <c>Payment</c> needs to
    /// satisfy the database constraint) and mocks the cache lookup the handler uses to validate it up
    /// front — mirrors <c>Location.Tests</c>' <c>CreateLocationCommandHandlerTests.CacheReturning</c>.
    /// </summary>
    private static async Task<Mock<IOrderTypeCache>> CacheReturningAsync(OrdersTestHost host, string paymentTypeId)
    {
        var type = OrderType.Create(paymentTypeId, OrderTypeCategory.Payment, paymentTypeId);
        await host.Context.OrderTypes.AddAsync(type, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var mock = new Mock<IOrderTypeCache>();
        mock
            .Setup(x => x.GetAsync(paymentTypeId, OrderTypeCategory.Payment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderType.Create(paymentTypeId, OrderTypeCategory.Payment, paymentTypeId));
        return mock;
    }

    /// <summary>
    /// Stateful <see cref="ICacheService"/> mock (backed by a local field standing in for whatever the
    /// real distributed/memory cache implementation would store) so a real <see cref="OrderTypeCache"/>
    /// can be exercised against it — see <see cref="Handle_ShouldReturnNotFound_WhenIdBelongsToAFeeCategoryType"/>.
    /// Mirrors <c>Services.OrderTypeCacheTests.CreateStatefulCacheMock</c>.
    /// </summary>
    private static ICacheService CreateStatefulCacheMock()
    {
        List<OrderType>? stored = null;

        var mock = new Mock<ICacheService>();

        mock.Setup(x => x.GetAsync<List<OrderType>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored!);

        mock.Setup(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<OrderType>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, List<OrderType>, TimeSpan?, CancellationToken>((_, value, _, _) => stored = value)
            .Returns(Task.CompletedTask);

        return mock.Object;
    }
}
