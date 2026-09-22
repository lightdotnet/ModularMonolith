using Light.Exceptions;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Domain.Orders;
using StarterKit.Orders.Contracts.Common;
using Xunit;

namespace Orders.Tests.Domain.Orders;

public class OrderDiscountTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Ctor_ShouldThrowValidationException_WhenPercentageIsOutOfRange(decimal value)
    {
        Assert.Throws<ValidationException>(() => new OrderDiscount(OrderDiscountKind.Percentage, value));
    }

    [Fact]
    public void Ctor_ShouldThrowValidationException_WhenFixedAmountIsNegative()
    {
        Assert.Throws<ValidationException>(() => new OrderDiscount(OrderDiscountKind.FixedAmount, -1m));
    }

    [Fact]
    public void Ctor_ShouldSucceed_ForValidPercentageAndFixedAmount()
    {
        var percentage = new OrderDiscount(OrderDiscountKind.Percentage, 50m);
        var fixedAmount = new OrderDiscount(OrderDiscountKind.FixedAmount, 25m);

        Assert.Equal(50m, percentage.Value);
        Assert.Equal(25m, fixedAmount.Value);
    }

    [Fact]
    public void ComputeAmount_ShouldReturnValue_ForFixedAmount_RegardlessOfSubtotal()
    {
        var discount = new OrderDiscount(OrderDiscountKind.FixedAmount, 30m);

        Assert.Equal(30m, discount.ComputeAmount(1000m));
        Assert.Equal(30m, discount.ComputeAmount(10m));
    }

    [Fact]
    public void ComputeAmount_ShouldReturnPercentageOfSubtotal_ForPercentage()
    {
        var discount = new OrderDiscount(OrderDiscountKind.Percentage, 25m);

        Assert.Equal(50m, discount.ComputeAmount(200m));
    }

    [Fact]
    public void Update_ShouldMutateTheSameInstanceInPlace_RatherThanBeingReassigned()
    {
        // Arrange — this is the regression this test targets: Order.ApplyDiscount reuses the same
        // tracked OrderDiscount reference when one already exists, so Update must actually change
        // the values visible through that original reference.
        var discount = new OrderDiscount(OrderDiscountKind.FixedAmount, 10m);

        // Act
        discount.Update(OrderDiscountKind.Percentage, 15m);

        // Assert
        Assert.Equal(OrderDiscountKind.Percentage, discount.Kind);
        Assert.Equal(15m, discount.Value);
        Assert.Equal(30m, discount.ComputeAmount(200m));
    }

    [Fact]
    public void Update_ShouldReRunTheSameGuards_AsTheConstructor()
    {
        var discount = new OrderDiscount(OrderDiscountKind.FixedAmount, 10m);

        Assert.Throws<ValidationException>(() => discount.Update(OrderDiscountKind.Percentage, 150m));
    }

    [Fact]
    public void ApplyDiscount_ShouldMutateTheExistingDiscountInPlace_WhenCalledTwice()
    {
        // Arrange — end-to-end through Order.ApplyDiscount, the actual call site of the deviation
        // called out during implementation.
        var order = OrderBuilder.DraftWithLine(unitPrice: 100m, quantity: 2);
        order.ApplyDiscount(OrderDiscountKind.FixedAmount, 10m);
        var firstDiscountReference = order.Discount;

        // Act
        order.ApplyDiscount(OrderDiscountKind.Percentage, 25m);

        // Assert
        Assert.Same(firstDiscountReference, order.Discount);
        Assert.Equal(OrderDiscountKind.Percentage, order.Discount!.Kind);
        Assert.Equal(25m, order.Discount.Value);
        Assert.Equal(50m, order.DiscountAmount);
    }
}
