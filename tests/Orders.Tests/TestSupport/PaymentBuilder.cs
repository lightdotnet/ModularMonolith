using StarterKit.Orders.Api.Domain.Payments;
using StarterKit.Orders.Contracts.Common;
using StarterKit.Shared.Constants;
using StarterKit.Shared.ValueObjects;

namespace Orders.Tests.TestSupport;

/// <summary>Thin convenience wrapper around <see cref="Payment.Create"/> (already fully public).</summary>
internal static class PaymentBuilder
{
    public static Payment Build(
        long orderId,
        decimal amount,
        DateTimeOffset paidAt,
        PaymentMethod method = PaymentMethod.Cash,
        string? reference = null,
        string recordedByUserId = "user-1",
        string orderCode = "20260101TESTCODE1") =>
        Payment.Create(
            orderId,
            orderCode,
            new Money(amount, CurrencyConstants.Default),
            method,
            paidAt,
            reference,
            recordedByUserId);
}
