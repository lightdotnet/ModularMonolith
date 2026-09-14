using Light.Exceptions;
using Orders.Tests.TestSupport;
using StarterKit.Orders.Api.Domain.Payments;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;
using Xunit;

namespace Orders.Tests.Domain.Payments;

public class PaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowValidationException_WhenOrderIdIsNotPositive(long orderId)
    {
        Assert.Throws<ValidationException>(() => Payment.Create(
            orderId,
            "20260101TESTCODE1",
            new Money(10m, CurrencyConstants.Default),
            PaymentMethod.Cash,
            Now,
            null,
            "user-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowValidationException_WhenRecordedByUserIdIsBlank(string recordedByUserId)
    {
        Assert.Throws<ValidationException>(() => Payment.Create(
            1L,
            "20260101TESTCODE1",
            new Money(10m, CurrencyConstants.Default),
            PaymentMethod.Cash,
            Now,
            null,
            recordedByUserId));
    }

    [Fact]
    public void Create_ShouldThrowValidationException_WhenAmountIsNotPositive()
    {
        Assert.Throws<ValidationException>(() => Payment.Create(
            1L,
            "20260101TESTCODE1",
            new Money(0m, CurrencyConstants.Default),
            PaymentMethod.Cash,
            Now,
            null,
            "user-1"));
    }

    [Fact]
    public void Create_ShouldSucceed_AndNotBeVoided()
    {
        // Act
        var payment = PaymentBuilder.Build(1L, 100m, Now, PaymentMethod.Card, "ref-1", "user-1", "20260101TESTCODE1");

        // Assert
        Assert.Equal(1L, payment.OrderId);
        Assert.Equal("20260101TESTCODE1", payment.OrderCode);
        Assert.Equal(100m, payment.Amount.Amount);
        Assert.Equal(PaymentMethod.Card, payment.Method);
        Assert.Equal(Now, payment.PaidAt);
        Assert.Equal("ref-1", payment.Reference);
        Assert.Equal("user-1", payment.RecordedByUserId);
        Assert.False(payment.IsVoided);
        Assert.Null(payment.VoidedAt);
        Assert.Null(payment.VoidReason);
    }

    [Fact]
    public void Void_ShouldThrowValidationException_WhenReasonIsBlank()
    {
        var payment = PaymentBuilder.Build(1L, 100m, Now);

        Assert.Throws<ValidationException>(() => payment.Void("   ", Now));
    }

    [Fact]
    public void Void_ShouldSucceed_AndSetVoidFields()
    {
        // Arrange
        var payment = PaymentBuilder.Build(1L, 100m, Now);

        // Act
        payment.Void("customer refund", Now);

        // Assert
        Assert.True(payment.IsVoided);
        Assert.Equal(Now, payment.VoidedAt);
        Assert.Equal("customer refund", payment.VoidReason);
    }

    [Fact]
    public void Void_ShouldThrowConflictException_WhenAlreadyVoided()
    {
        // Arrange
        var payment = PaymentBuilder.Build(1L, 100m, Now);
        payment.Void("first void", Now);

        // Act & Assert
        Assert.Throws<ConflictException>(() => payment.Void("second void", Now));
    }
}
