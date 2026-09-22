using Light.Exceptions;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Api.Domain.Payments;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Domain.Orders;

public class OrderCurrencyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CatalogPriceSnapshot UsdSnapshot(
        decimal amount = 10m,
        decimal rate = 25_000m) =>
        new(new Money(amount, "USD"), rate, CurrencyServiceMock.RateEffectiveFrom);

    [Fact]
    public void Create_ShouldSetCurrencyCodeAndAmountPaidCurrency_FromTheSuppliedCode()
    {
        // Act
        var order = Order.Create("location-1", null, "usd", Now);

        // Assert
        Assert.Equal("USD", order.CurrencyCode);
        Assert.Equal("USD", order.AmountPaid.Currency);
        Assert.Equal(0m, order.AmountPaid.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("US1")]
    public void Create_ShouldThrowValidationException_WhenCurrencyCodeIsMalformed(string currencyCode)
    {
        Assert.Throws<ValidationException>(() => Order.Create("location-1", null, currencyCode, Now));
    }

    [Fact]
    public void CurrencyCode_ShouldHaveNoPublicSetter_AndSurviveStateTransitions()
    {
        // Arrange
        var order = OrderBuilder.Draft(currencyCode: "USD");
        OrderBuilder.AddLine(order, currency: "USD");

        // Act
        order.Place(Now);
        order.ReconcilePaymentStatus(5m);

        // Assert
        Assert.False(typeof(Order).GetProperty(nameof(Order.CurrencyCode))!.SetMethod!.IsPublic);
        Assert.Equal("USD", order.CurrencyCode);
        Assert.Equal("USD", order.AmountPaid.Currency);
    }

    [Fact]
    public void AddLine_ShouldThrowValidationException_WhenUnitPriceIsNotInTheOrderCurrency()
    {
        var order = OrderBuilder.Draft();

        Assert.Throws<ValidationException>(() => OrderBuilder.AddLine(order, currency: "USD"));
        Assert.Empty(order.Lines);
    }

    [Fact]
    public void AddLine_ShouldThrowValidationException_WhenSalePriceIsNotInTheOrderCurrency()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act / Assert
        Assert.Throws<ValidationException>(() => order.AddLine(
            1,
            "Widget",
            "SKU-1",
            1,
            new Money(100m, "VND"),
            new VatPercentage(10m),
            new Money(50m, "USD")));
        Assert.Empty(order.Lines);
    }

    [Fact]
    public void SetLineSalePrice_ShouldThrowValidationException_WhenSalePriceIsNotInTheOrderCurrency()
    {
        // Arrange
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m);

        // Act / Assert
        Assert.Throws<ValidationException>(() => order.SetLineSalePrice(0, new Money(50m, "USD")));
    }

    [Fact]
    public void AddFee_ShouldThrowValidationException_WhenAmountIsNotInTheOrderCurrency()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act / Assert
        Assert.Throws<ValidationException>(() => order.AddFee("Shipping", new Money(10m, "USD"), "SHIPPING", "Shipping"));
        Assert.Empty(order.Fees);
    }

    [Fact]
    public void AddFee_ShouldAccept_WhenAmountIsInTheOrderCurrency()
    {
        // Arrange
        var order = OrderBuilder.Draft(currencyCode: "USD");

        // Act
        order.AddFee("Shipping", new Money(10m, "USD"), "SHIPPING", "Shipping");

        // Assert
        Assert.Equal("USD", Assert.Single(order.Fees).Amount.Currency);
    }

    [Fact]
    public void PaymentCreate_ShouldThrowValidationException_WhenAmountIsNotInTheOrderCurrency()
    {
        Assert.Throws<ValidationException>(() => Payment.Create(
            1L,
            "20260101TESTCODE1",
            new Money(10m, "USD"),
            "VND",
            "CASH",
            "Cash",
            Now,
            null,
            "user-1"));
    }

    [Fact]
    public void PaymentCreate_ShouldAccept_WhenAmountIsInTheOrderCurrency()
    {
        var payment = Payment.Create(
            1L,
            "20260101TESTCODE1",
            new Money(10m, "USD"),
            "USD",
            "CASH",
            "Cash",
            Now,
            null,
            "user-1");

        Assert.Equal("USD", payment.Amount.Currency);
    }

    [Fact]
    public void AddLine_ShouldStoreTheSnapshot_WhenTheCatalogCurrencyDiffersFromTheOrderCurrency()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act
        OrderBuilder.AddLine(order, unitPrice: 250_000m, catalogPrice: UsdSnapshot());

        // Assert
        var line = Assert.Single(order.Lines);
        Assert.Equal(250_000m, line.UnitPrice.Amount);
        Assert.Equal("VND", line.UnitPrice.Currency);
        Assert.Equal(10m, line.CatalogUnitPrice);
        Assert.Equal("USD", line.CatalogCurrency);
        Assert.Equal(25_000m, line.AppliedRate);
        Assert.Equal(CurrencyServiceMock.RateEffectiveFrom, line.RateEffectiveFrom);
    }

    [Fact]
    public void AddLine_ShouldLeaveTheSnapshotNull_WhenNoneIsSupplied()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act
        OrderBuilder.AddLine(order);

        // Assert
        var line = Assert.Single(order.Lines);
        Assert.Null(line.CatalogUnitPrice);
        Assert.Null(line.CatalogCurrency);
        Assert.Null(line.AppliedRate);
        Assert.Null(line.RateEffectiveFrom);
    }

    [Fact]
    public void AddLine_ShouldThrowValidationException_WhenTheSnapshotCurrencyEqualsTheOrderCurrency()
    {
        // Arrange
        var order = OrderBuilder.Draft();
        var sameCurrency = new CatalogPriceSnapshot(new Money(10m, "VND"), 1m, null);

        // Act / Assert
        Assert.Throws<ValidationException>(() => OrderBuilder.AddLine(order, catalogPrice: sameCurrency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddLine_ShouldThrowValidationException_WhenTheAppliedRateIsNotPositive(decimal rate)
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act / Assert
        Assert.Throws<ValidationException>(() => OrderBuilder.AddLine(
            order,
            unitPrice: 250_000m,
            catalogPrice: UsdSnapshot(rate: rate)));
        Assert.Empty(order.Lines);
    }

    [Fact]
    public void AddLine_ShouldCapTheSalePrice_AgainstTheConvertedUnitPrice()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act / Assert: capped by the converted 250,000 VND, not by the 10 USD catalog price.
        Assert.Throws<ValidationException>(() => OrderBuilder.AddLine(
            order,
            unitPrice: 250_000m,
            requestedSalePrice: 250_001m,
            catalogPrice: UsdSnapshot()));

        OrderBuilder.AddLine(
            order,
            unitPrice: 250_000m,
            requestedSalePrice: 200_000m,
            catalogPrice: UsdSnapshot());

        Assert.Equal(200_000m, Assert.Single(order.Lines).RequestedSalePrice!.Amount);
    }

    [Fact]
    public void UpdateLineQuantity_ShouldNotTouchThePriceOrTheSnapshot()
    {
        // Arrange
        var order = OrderBuilder.Draft();
        OrderBuilder.AddLine(order, unitPrice: 250_000m, catalogPrice: UsdSnapshot());
        var line = order.Lines[0];

        // Act
        order.UpdateLineQuantity(line.Id, 5);

        // Assert
        Assert.Equal(5, line.Quantity);
        Assert.Equal(250_000m, line.UnitPrice.Amount);
        Assert.Equal(10m, line.CatalogUnitPrice);
        Assert.Equal(25_000m, line.AppliedRate);
        Assert.Equal(CurrencyServiceMock.RateEffectiveFrom, line.RateEffectiveFrom);
    }
}
