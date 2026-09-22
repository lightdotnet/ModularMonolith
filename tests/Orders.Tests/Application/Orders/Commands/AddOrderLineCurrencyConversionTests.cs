using Light.Exceptions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Orders.Tests.TestSupport;
using StarterKit.Catalog.Contracts.Common;
using StarterKit.Catalog.Contracts.Products;
using StarterKit.Catalog.Contracts.Services;
using StarterKit.Currencies.Contracts.Services;
using StarterKit.Orders.Api.Application.Orders.Commands;
using StarterKit.Orders.Contracts.Orders;
using Xunit;

namespace Orders.Tests.Application.Orders.Commands;

/// <summary>
/// Multi-currency behaviour of <see cref="AddOrderLineCommandHandler"/>: a product priced in a foreign
/// currency is converted once into the order (base) currency, with the conversion kept as a snapshot.
/// </summary>
public class AddOrderLineCurrencyConversionTests
{
    private static ProductPriceInfoDto PriceInfo(
        decimal price,
        string currency) => new()
    {
        Id = 1,
        ProductName = "Widget",
        Sku = "SKU-1",
        Price = price,
        Currency = currency,
        VatRate = 10m,
        Status = ProductStatus.Active,
    };

    private static AddOrderLineCommandHandler MakeHandler(
        OrdersTestHost host,
        ProductPriceInfoDto priceInfo,
        ICurrencyService currencyService)
    {
        var pricingMock = new Mock<ICatalogPricingService>();
        pricingMock
            .Setup(s => s.GetPriceInfoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceInfo);

        return new AddOrderLineCommandHandler(
            host.Context,
            pricingMock.Object,
            currencyService,
            host.DateTime);
    }

    private static async Task<long> SeedDraftAsync(
        OrdersTestHost host,
        string currencyCode = "VND")
    {
        var order = OrderBuilder.Draft(currencyCode: currencyCode);
        await host.Context.Orders.AddAsync(order, TestContext.Current.CancellationToken);
        await host.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return order.Id;
    }

    private static async Task<StarterKit.Orders.Api.Domain.Orders.Order> ReloadAsync(
        OrdersTestHost host,
        long orderId)
    {
        host.Context.ChangeTracker.Clear();

        return await host.Context.Orders
            .Include(x => x.Lines)
            .FirstAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldConvertAForeignPriceIntoTheOrderCurrency_AndSnapshotIt()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 25_000m });
        var handler = MakeHandler(host, PriceInfo(10m, "USD"), currency.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 2 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(250_000m, line.UnitPrice.Amount);
        Assert.Equal("VND", line.UnitPrice.Currency);
        Assert.Equal(10m, line.CatalogUnitPrice);
        Assert.Equal("USD", line.CatalogCurrency);
        Assert.Equal(25_000m, line.AppliedRate);
        Assert.Equal(CurrencyServiceMock.RateEffectiveFrom, line.RateEffectiveFrom);
    }

    [Fact]
    public async Task Handle_ShouldResolveTheRateAtTheCurrentTime()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 25_000m });
        var handler = MakeHandler(host, PriceInfo(10m, "USD"), currency.Object);

        // Act
        await handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        currency.Verify(
            x => x.GetRateToBaseAsync("USD", host.DateTime.UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRoundTheConvertedPriceAwayFromZero_UsingTheBaseDecimalPlaces()
    {
        // Arrange: 10 x 2,500.05 = 25,000.50, a midpoint that banker's rounding would send to 25,000.
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 2_500.05m });
        var handler = MakeHandler(host, PriceInfo(10m, "USD"), currency.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(25_001m, line.UnitPrice.Amount);
        Assert.Equal(2_500.05m, line.AppliedRate);
    }

    [Fact]
    public async Task Handle_ShouldKeepBaseDecimalPlaces_WhenTheBaseCurrencyHasTwoDecimals()
    {
        // Arrange: base USD (2 decimals), product priced in EUR at 1.0837 -> 10.837 rounds to 10.84.
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host, "USD");
        var currency = CurrencyServiceMock.Create(
            baseCode: "USD",
            baseDecimals: 2,
            rates: new Dictionary<string, decimal> { ["EUR"] = 1.0837m });
        var handler = MakeHandler(host, PriceInfo(10m, "EUR"), currency.Object);

        // Act
        await handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(10.84m, line.UnitPrice.Amount);
        Assert.Equal("USD", line.UnitPrice.Currency);
    }

    [Fact]
    public async Task Handle_ShouldLeaveTheSnapshotNullAndSkipTheRateLookup_WhenThePriceIsAlreadyInTheOrderCurrency()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create();
        var handler = MakeHandler(host, PriceInfo(100m, "VND"), currency.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(100m, line.UnitPrice.Amount);
        Assert.Null(line.CatalogUnitPrice);
        Assert.Null(line.CatalogCurrency);
        Assert.Null(line.AppliedRate);
        Assert.Null(line.RateEffectiveFrom);
        currency.Verify(
            x => x.GetRateToBaseAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowExchangeRateNotFound_AndPersistNothing_WhenNoRateExists()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create();
        var handler = MakeHandler(host, PriceInfo(10m, "EUR"), currency.Object);

        // Act / Assert
        await Assert.ThrowsAsync<ExchangeRateNotFoundException>(() => handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        // Nothing was added to the tracked aggregate, and nothing reached the database.
        Assert.Empty(host.Context.Orders.Local.Single().Lines);
        Assert.Empty(host.Context.ChangeTracker.Entries<StarterKit.Orders.Api.Domain.Orders.OrderLine>());
        var reloaded = await ReloadAsync(host, orderId);
        Assert.Empty(reloaded.Lines);
        Assert.Equal(0, await host.Context.OrderLines.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenTheOrderCurrencyIsNotTheBaseCurrency()
    {
        // Arrange: a USD order while the base currency is VND.
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host, "USD");
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["EUR"] = 1.1m });
        var handler = MakeHandler(host, PriceInfo(10m, "EUR"), currency.Object);

        // Act / Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        Assert.Empty((await ReloadAsync(host, orderId)).Lines);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenTheBaseCurrencyHasMoreThanTwoDecimals_EvenForASameCurrencyLine()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host, "KWD");
        var currency = CurrencyServiceMock.Create(baseCode: "KWD", baseDecimals: 3);
        var handler = MakeHandler(host, PriceInfo(1.5m, "KWD"), currency.Object);

        // Act / Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        Assert.Contains("3 decimal places", exception.Message);
        Assert.Empty((await ReloadAsync(host, orderId)).Lines);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_AndPersistNothing_WhenTheConvertedPriceOverflowsTheColumn()
    {
        // Arrange: 9,000,000,000,000 x 10,000 = 9e16, above decimal(18,2).
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 10_000m });
        var handler = MakeHandler(host, PriceInfo(9_000_000_000_000m, "USD"), currency.Object);

        // Act / Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        Assert.Empty((await ReloadAsync(host, orderId)).Lines);
    }

    [Fact]
    public async Task Handle_ShouldThrowValidationException_WhenTheConvertedPriceRoundsToZero()
    {
        // Arrange: 0.01 x 0.001 = 0.00001, which rounds to 0 VND.
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 0.001m });
        var handler = MakeHandler(host, PriceInfo(0.01m, "USD"), currency.Object);

        // Act / Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        Assert.Empty((await ReloadAsync(host, orderId)).Lines);
    }

    [Fact]
    public async Task Handle_ShouldAcceptAZeroCatalogPrice_AsAZeroPricedLine()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 25_000m });
        var handler = MakeHandler(host, PriceInfo(0m, "USD"), currency.Object);

        // Act
        var result = await handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(0m, line.UnitPrice.Amount);
        Assert.Equal("USD", line.CatalogCurrency);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflict_WhenTheQuoteIsAgainstAnotherBaseCurrency()
    {
        // Arrange: base changed between the two Currency calls, so the quote is against USD.
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create();
        currency
            .Setup(x => x.GetRateToBaseAsync(
                "EUR",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateQuote("EUR", "USD", 1.1m, CurrencyServiceMock.RateEffectiveFrom));
        var handler = MakeHandler(host, PriceInfo(10m, "EUR"), currency.Object);

        // Act / Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken));

        Assert.Empty((await ReloadAsync(host, orderId)).Lines);
    }

    [Fact]
    public async Task Handle_ShouldCapTheSalePrice_AgainstTheConvertedRoundedUnitPrice()
    {
        // Arrange: converted unit price is 250,000 VND.
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 25_000m });
        var handler = MakeHandler(host, PriceInfo(10m, "USD"), currency.Object);

        // Act / Assert: above the converted price is rejected, at or below it is accepted.
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new AddOrderLineCommand(
                orderId,
                new AddOrderLineRequest { ProductId = 1, Quantity = 1, RequestedSalePrice = 250_001m }),
            TestContext.Current.CancellationToken));

        var result = await handler.Handle(
            new AddOrderLineCommand(
                orderId,
                new AddOrderLineRequest { ProductId = 1, Quantity = 1, RequestedSalePrice = 240_000m }),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(240_000m, line.RequestedSalePrice!.Amount);
        Assert.Equal("VND", line.RequestedSalePrice.Currency);
    }

    [Fact]
    public async Task UpdateQuantityHandler_ShouldNotReconvertOrChangeTheSnapshot()
    {
        // Arrange
        using var host = new OrdersTestHost();
        var orderId = await SeedDraftAsync(host);
        var currency = CurrencyServiceMock.Create(rates: new Dictionary<string, decimal> { ["USD"] = 25_000m });
        var addHandler = MakeHandler(host, PriceInfo(10m, "USD"), currency.Object);
        await addHandler.Handle(
            new AddOrderLineCommand(orderId, new AddOrderLineRequest { ProductId = 1, Quantity = 1 }),
            TestContext.Current.CancellationToken);
        var lineId = (await ReloadAsync(host, orderId)).Lines[0].Id;
        var updateHandler = new UpdateOrderLineQuantityCommandHandler(host.Context);

        // Act
        var result = await updateHandler.Handle(
            new UpdateOrderLineQuantityCommand(
                orderId,
                lineId,
                new UpdateOrderLineQuantityRequest { Quantity = 4 }),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var line = Assert.Single((await ReloadAsync(host, orderId)).Lines);
        Assert.Equal(4, line.Quantity);
        Assert.Equal(250_000m, line.UnitPrice.Amount);
        Assert.Equal(25_000m, line.AppliedRate);
        currency.Verify(
            x => x.GetRateToBaseAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
