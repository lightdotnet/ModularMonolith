using Light.Exceptions;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Domain.Orders;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowValidationException_WhenLocationIdIsBlank(string locationId)
    {
        Assert.Throws<ValidationException>(() => Order.Create(locationId, null, Now));
    }

    [Fact]
    public void Create_ShouldSucceed_WithDraftStatusAndZeroAmountPaid()
    {
        // Act
        var order = Order.Create("location-1", "member-1", Now);

        // Assert
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal("location-1", order.LocationId);
        Assert.Equal("member-1", order.MemberId);
        Assert.Equal(0m, order.AmountPaid.Amount);
        Assert.Equal(CurrencyConstants.Default, order.AmountPaid.Currency);
    }

    [Fact]
    public void Create_ShouldGenerateAnOrderCode_WhenNoneIsSupplied()
    {
        // Act
        var order = Order.Create("location-1", null, Now);

        // Assert — yyyyMMdd + 9-char Crockford Base32 suffix, per OrderCode's own doc comment.
        Assert.Equal(17, order.OrderCode.Value.Length);
        Assert.StartsWith("20260101", order.OrderCode.Value);
        Assert.Null(order.ExternalReferenceCode);
    }

    [Fact]
    public void Create_ShouldUseTheSuppliedOrderCode_WhenOneIsGiven()
    {
        // Act
        var order = Order.Create("location-1", null, Now, new OrderCode("CUSTOM-CODE-1"), "EXT-REF-1");

        // Assert
        Assert.Equal("CUSTOM-CODE-1", order.OrderCode.Value);
        Assert.Equal("EXT-REF-1", order.ExternalReferenceCode);
    }

    [Fact]
    public void RegenerateOrderCode_ShouldReplaceTheOrderCode_WithANewlyGeneratedOne()
    {
        // Arrange
        var order = Order.Create("location-1", null, Now, new OrderCode("ORIGINAL-CODE"));

        // Act
        order.RegenerateOrderCode(Now.AddDays(1));

        // Assert
        Assert.NotEqual("ORIGINAL-CODE", order.OrderCode.Value);
        Assert.StartsWith("20260102", order.OrderCode.Value);
    }

    [Fact]
    public void AddLine_ShouldSnapshotTheOrderCode_OntoTheNewLine()
    {
        // Arrange
        var order = Order.Create("location-1", null, Now, new OrderCode("SNAPSHOT-CODE"));

        // Act
        OrderBuilder.AddLine(order);

        // Assert
        Assert.Equal("SNAPSHOT-CODE", order.Lines[0].OrderCode);
    }

    [Fact]
    public void AddFee_ShouldSnapshotTheOrderCode_OntoTheNewFee()
    {
        // Arrange
        var order = Order.Create("location-1", null, Now, new OrderCode("SNAPSHOT-CODE"));

        // Act
        order.AddFee("Shipping", new Money(1m, CurrencyConstants.Default), "SHIPPING", "Shipping");

        // Assert
        Assert.Equal("SNAPSHOT-CODE", order.Fees[0].OrderCode);
    }

    public static IEnumerable<object[]> DraftOnlyGuardedActions()
    {
        yield return [(Action<Order>)(o => OrderBuilder.AddLine(o))];
        yield return [(Action<Order>)(o => o.ApplyDiscount(OrderDiscountKind.FixedAmount, 1m))];
        yield return [(Action<Order>)(o => o.RemoveDiscount())];
        yield return [(Action<Order>)(o => o.AddFee("Shipping", new Money(1m, CurrencyConstants.Default), "SHIPPING", "Shipping"))];
    }

    [Theory]
    [MemberData(nameof(DraftOnlyGuardedActions))]
    public void DraftOnlyActions_ShouldThrowConflictException_WhenOrderIsNotDraft(Action<Order> action)
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => action(order));
    }

    [Fact]
    public void UpdateLineQuantity_ShouldThrowConflictException_WhenOrderIsNotDraft()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);
        var lineId = order.Lines[0].Id;

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.UpdateLineQuantity(lineId, 2));
    }

    [Fact]
    public void SetLineSalePrice_ShouldThrowConflictException_WhenOrderIsNotDraft()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);
        var lineId = order.Lines[0].Id;

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.SetLineSalePrice(lineId, null));
    }

    [Fact]
    public void RemoveLine_ShouldThrowConflictException_WhenOrderIsNotDraft()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);
        var lineId = order.Lines[0].Id;

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.RemoveLine(lineId));
    }

    [Fact]
    public void RemoveFee_ShouldThrowConflictException_WhenOrderIsNotDraft()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.RemoveFee(999));
    }

    [Fact]
    public void AddLine_ShouldThrowValidationException_WhenRequestedSalePriceExceedsUnitPrice()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act & Assert
        Assert.Throws<ValidationException>(() => OrderBuilder.AddLine(
            order,
            unitPrice: 100m,
            requestedSalePrice: 150m));
    }

    [Fact]
    public void AddLine_ShouldSucceed_AndAddTheLineSnapshot()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act
        OrderBuilder.AddLine(order, productId: 1, productName: "Widget", sku: "SKU-1", unitPrice: 50m, quantity: 3);

        // Assert
        var line = Assert.Single(order.Lines);
        Assert.Equal(1, line.ProductId);
        Assert.Equal("Widget", line.ProductName);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal(50m, line.UnitPrice.Amount);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(150m, order.Subtotal);
    }

    [Fact]
    public void SetLineSalePrice_ShouldThrowValidationException_WhenOrderLineNotFound()
    {
        // Arrange
        var order = OrderBuilder.DraftWithLine();

        // Act & Assert
        Assert.Throws<ValidationException>(() => order.SetLineSalePrice(999, null));
    }

    [Fact]
    public void SetLineSalePrice_ShouldThrowValidationException_WhenSalePriceExceedsUnitPrice()
    {
        // Arrange
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m);
        var lineId = order.Lines[0].Id;

        // Act & Assert
        Assert.Throws<ValidationException>(
            () => order.SetLineSalePrice(lineId, new Money(150m, CurrencyConstants.Default)));
    }

    [Fact]
    public void RemoveFee_ShouldThrowValidationException_WhenOrderFeeNotFound()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act & Assert
        Assert.Throws<ValidationException>(() => order.RemoveFee(999));
    }

    [Fact]
    public void Place_ShouldThrowValidationException_WhenThereAreNoLines()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act & Assert
        Assert.Throws<ValidationException>(() => order.Place(Now));
    }

    [Fact]
    public void Place_ShouldThrowValidationException_WhenDiscountExceedsSubtotal()
    {
        // Arrange — ApplyDiscount itself allows this; the cap is only enforced at Place time.
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 1);
        order.ApplyDiscount(OrderDiscountKind.FixedAmount, 200m);

        // Act & Assert
        Assert.Throws<ValidationException>(() => order.Place(Now));
    }

    [Fact]
    public void Place_ShouldThrowConflictException_WhenOrderIsNotDraft()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.Place(Now));
    }

    [Fact]
    public void Place_ShouldSucceed_AndRaiseOrderPlacedEvent()
    {
        // Arrange
        var order = OrderBuilder.DraftWithLine();

        // Act
        order.Place(Now);

        // Assert
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(Now, order.PlacedAt);
        var raised = Assert.Single(order.DomainEvents.OfType<OrderPlacedEvent>());
        Assert.Equal(order.Id, raised.OrderId);
        Assert.Equal(order.LocationId, raised.LocationId);
        Assert.Equal(Now, raised.PlacedAt);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Fulfilled)]
    public void Cancel_ShouldThrowConflictException_WhenOrderIsPaidOrFulfilled(OrderStatus status)
    {
        // Arrange
        var order = OrderBuilder.Paid(Now);

        if (status == OrderStatus.Fulfilled)
            order.MarkFulfilled(Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.Cancel("user-1", "changed my mind", Now));
    }

    [Fact]
    public void Cancel_ShouldThrowValidationException_WhenReasonIsBlank()
    {
        // Arrange
        var order = OrderBuilder.Draft();

        // Act & Assert
        Assert.Throws<ValidationException>(() => order.Cancel("user-1", "   ", Now));
    }

    [Fact]
    public void Cancel_ShouldSucceed_AndRaiseOrderCancelledEvent()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);

        // Act
        order.Cancel("user-1", "customer changed mind", Now);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(Now, order.CancelledAt);
        Assert.Equal("customer changed mind", order.CancelledReason);
        var raised = Assert.Single(order.DomainEvents.OfType<OrderCancelledEvent>());
        Assert.Equal(order.Id, raised.OrderId);
        Assert.Equal("user-1", raised.CancelledByUserId);
        Assert.Equal("customer changed mind", raised.Reason);
    }

    [Fact]
    public void MarkFulfilled_ShouldThrowConflictException_WhenOrderIsNotPaid()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => order.MarkFulfilled(Now));
    }

    [Fact]
    public void MarkFulfilled_ShouldSucceed_AndRaiseOrderFulfilledEvent()
    {
        // Arrange
        var order = OrderBuilder.Paid(Now);

        // Act
        order.MarkFulfilled(Now);

        // Assert
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
        Assert.Equal(Now, order.FulfilledAt);
        var raised = Assert.Single(order.DomainEvents.OfType<OrderFulfilledEvent>());
        Assert.Equal(order.Id, raised.OrderId);
    }

    [Fact]
    public void ReconcilePaymentStatus_ShouldThrowConflictException_WhenOrderIsDraftOrCancelled()
    {
        // Arrange
        var draft = OrderBuilder.Draft();
        var cancelled = OrderBuilder.Placed(Now);
        cancelled.Cancel("user-1", "reason", Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => draft.ReconcilePaymentStatus(0m));
        Assert.Throws<ConflictException>(() => cancelled.ReconcilePaymentStatus(0m));
    }

    [Theory]
    [InlineData(0, OrderStatus.Placed)]
    [InlineData(50, OrderStatus.PartiallyPaid)]
    [InlineData(100, OrderStatus.Paid)]
    [InlineData(150, OrderStatus.Paid)]
    public void ReconcilePaymentStatus_ShouldDeriveStatus_FromTotalPaidAgainstTotal(
        decimal totalPaid,
        OrderStatus expectedStatus)
    {
        // Arrange — Total is 100 (one line, unit price 100, quantity 1).
        var order = OrderBuilder.Placed(Now, unitPrice: 100m, quantity: 1);

        // Act
        order.ReconcilePaymentStatus(totalPaid);

        // Assert
        Assert.Equal(expectedStatus, order.Status);
        Assert.Equal(totalPaid, order.AmountPaid.Amount);
    }

    [Fact]
    public void ReconcilePaymentStatus_ShouldThrowValidationException_WhenTotalPaidIsNegative()
    {
        // Arrange — AmountPaid.Update (Money's own negative-amount guard) runs before the
        // status-deriving switch below it, so a negative totalPaid never reaches (and the
        // switch's own "<= 0 => Placed" arm can, in practice, only ever be hit by exactly zero).
        var order = OrderBuilder.Placed(Now, unitPrice: 100m, quantity: 1);

        // Act & Assert
        Assert.Throws<ValidationException>(() => order.ReconcilePaymentStatus(-10m));
    }

    [Fact]
    public void ReconcilePaymentStatus_ShouldBeIdempotent_WhenCalledTwiceWithTheSameValue()
    {
        // Arrange
        var order = OrderBuilder.Placed(Now, unitPrice: 100m, quantity: 1);

        // Act
        order.ReconcilePaymentStatus(50m);
        order.ReconcilePaymentStatus(50m);

        // Assert
        Assert.Equal(OrderStatus.PartiallyPaid, order.Status);
        Assert.Equal(50m, order.AmountPaid.Amount);
    }

    [Fact]
    public void ReconcilePaymentStatus_ShouldNeverRegressFulfilled_ButStillRefreshesAmountPaid()
    {
        // Arrange
        var order = OrderBuilder.Paid(Now, unitPrice: 100m, quantity: 1);
        order.MarkFulfilled(Now);

        // Act — simulate a payment being voided after fulfillment.
        order.ReconcilePaymentStatus(0m);

        // Assert
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
        Assert.Equal(0m, order.AmountPaid.Amount);
    }

    [Fact]
    public void Total_ShouldBeSubtotalMinusDiscountPlusFees()
    {
        // Arrange
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        order.ApplyDiscount(OrderDiscountKind.FixedAmount, 20m);
        order.AddFee("Shipping", new Money(10m, CurrencyConstants.Default), "SHIPPING", "Shipping");

        // Assert
        Assert.Equal(200m, order.Subtotal);
        Assert.Equal(20m, order.DiscountAmount);
        Assert.Equal(10m, order.FeesTotal);
        Assert.Equal(190m, order.Total);
    }
}
