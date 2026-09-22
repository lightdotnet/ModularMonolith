using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Locations.Contracts.Services;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Api.Application.Orders.Queries;
using StarterKit.Orders.Api.Application.Payments.Commands;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Api.Domain.OrderTypes;
using StarterKit.Orders.Api.Services;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Orders.Contracts.Orders;
using StarterKit.Orders.Contracts.Payments;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Application.Orders;

/// <summary>Handlers that used to hard-code the default currency now follow the order's own currency.</summary>
public class OrderCurrencyHandlerTests
{
    private static async Task<Order> SeedAsync(
        OrdersTestHost host,
        Order order)
    {
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return order;
    }

    private static Mock<IOrderTypeCache> CacheFor(
        string id,
        OrderTypeCategory category)
    {
        var mock = new Mock<IOrderTypeCache>();
        mock
            .Setup(x => x.GetAsync(id, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderType.Create(id, category, id));
        return mock;
    }

    [Theory]
    [InlineData("VND", 0)]
    [InlineData("USD", 2)]
    public async Task CreateOrder_ShouldUseTheBaseCurrencyCode(
        string baseCode,
        int baseDecimals)
    {
        // Arrange
        using var host = new OrdersTestHost();
        var locationMock = new Mock<ILocationDirectoryService>();
        locationMock
            .Setup(s => s.ExistsAsync("location-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreateOrderCommandHandler(
            host.Context,
            locationMock.Object,
            CurrencyServiceMock.Create(baseCode, baseDecimals).Object,
            host.DateTime,
            NullLogger<CreateOrderCommandHandler>.Instance);

        // Act
        var result = await handler.Handle(
            new CreateOrderCommand(new CreateOrderRequest { LocationId = "location-1" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var order = await host.Context.Orders.FirstAsync(x => x.Id == result.Data, TestContext.Current.CancellationToken);
        Assert.Equal(baseCode, order.CurrencyCode);
        Assert.Equal(baseCode, order.AmountPaid.Currency);
    }

    [Fact]
    public async Task AddOrderFee_ShouldUseTheOrderCurrency()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.Draft(currencyCode: "USD"));
        var handler = new AddOrderFeeCommandHandler(host.Context, CacheFor("SHIPPING", OrderTypeCategory.Fee).Object);

        // Act
        var result = await handler.Handle(
            new AddOrderFeeCommand(
                order.Id,
                new AddOrderFeeRequest { Name = "Shipping", Amount = 10m, FeeTypeId = "SHIPPING" }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        host.Context.ChangeTracker.Clear();
        var fee = await host.Context.OrderFees.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("USD", fee.Amount.Currency);
    }

    [Fact]
    public async Task SetOrderLineSalePrice_ShouldUseTheOrderCurrency()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var draft = OrderBuilder.Draft(currencyCode: "USD");
        OrderBuilder.AddLine(draft, unitPrice: 100m, currency: "USD");
        var order = await SeedAsync(host, draft);
        var handler = new SetOrderLineSalePriceCommandHandler(host.Context);

        // Act
        var result = await handler.Handle(
            new SetOrderLineSalePriceCommand(
                order.Id,
                order.Lines[0].Id,
                new SetOrderLineSalePriceRequest { SalePrice = 60m }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        host.Context.ChangeTracker.Clear();
        var line = await host.Context.OrderLines.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("USD", line.RequestedSalePrice!.Currency);
        Assert.Equal(60m, line.RequestedSalePrice.Amount);
    }

    [Fact]
    public async Task RecordPayment_ShouldAccept_WhenTheCurrencyEqualsTheOrderCurrency()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var draft = OrderBuilder.Draft(currencyCode: "USD");
        OrderBuilder.AddLine(draft, unitPrice: 100m, currency: "USD");
        draft.Place(host.DateTime.UtcNow);
        var order = await SeedAsync(host, draft);
        var handler = new RecordPaymentCommandHandler(host.Context, CacheFor("CASH", OrderTypeCategory.Payment).Object);

        // Act
        var result = await handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 40m,
                    Currency = "USD",
                    PaymentTypeId = "CASH",
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var payment = await host.Context.Payments.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("USD", payment.Amount.Currency);
    }

    [Fact]
    public async Task RecordPayment_ShouldThrowValidationException_AndPersistNothing_WhenTheCurrencyDiffersFromTheOrderCurrency()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var order = await SeedAsync(host, OrderBuilder.Placed(host.DateTime.UtcNow, unitPrice: 100m));
        var handler = new RecordPaymentCommandHandler(host.Context, CacheFor("CASH", OrderTypeCategory.Payment).Object);

        // Act / Assert: the order is VND, the payment claims USD.
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new RecordPaymentCommand(
                order.Id,
                new RecordPaymentRequest
                {
                    Amount = 40m,
                    Currency = "USD",
                    PaymentTypeId = "CASH",
                    PaidAt = host.DateTime.UtcNow,
                },
                "user-1"),
            TestContext.Current.CancellationToken));

        Assert.Empty(host.Context.Payments);
    }

    [Fact]
    public async Task GetOrderById_ShouldMapTheCurrencyAndTheLineSnapshotFields()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var draft = OrderBuilder.Draft();
        OrderBuilder.AddLine(
            draft,
            productId: 1,
            unitPrice: 250_000m,
            catalogPrice: new CatalogPriceSnapshot(
                new Money(10m, "USD"),
                25_000m,
                CurrencyServiceMock.RateEffectiveFrom));
        OrderBuilder.AddLine(draft, productId: 2, sku: "SKU-2", unitPrice: 100m);
        var order = await SeedAsync(host, draft);
        var handler = new GetOrderByIdQueryHandler(host.Context);

        // Act
        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Data!;
        Assert.Equal("VND", dto.Currency);
        var foreign = dto.Lines.Single(x => x.ProductId == 1);
        Assert.Equal(250_000m, foreign.UnitPrice);
        Assert.Equal(10m, foreign.CatalogUnitPrice);
        Assert.Equal("USD", foreign.CatalogCurrency);
        Assert.Equal(25_000m, foreign.AppliedRate);
        Assert.Equal(CurrencyServiceMock.RateEffectiveFrom, foreign.RateEffectiveFrom);
        var local = dto.Lines.Single(x => x.ProductId == 2);
        Assert.Null(local.CatalogUnitPrice);
        Assert.Null(local.CatalogCurrency);
        Assert.Null(local.AppliedRate);
        Assert.Null(local.RateEffectiveFrom);
    }
}
