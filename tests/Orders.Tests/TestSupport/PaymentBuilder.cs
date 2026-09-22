using StarterKit.Orders.Api.Domain.Payments;
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
        string paymentTypeId = "CASH",
        string? reference = null,
        string recordedByUserId = "user-1",
        string orderCode = "20260101TESTCODE1",
        string paymentTypeName = "Cash",
        string orderCurrencyCode = CurrencyConstants.Default,
        string currency = CurrencyConstants.Default) =>
        Payment.Create(
            orderId,
            orderCode,
            new Money(amount, currency),
            orderCurrencyCode,
            paymentTypeId,
            paymentTypeName,
            paidAt,
            reference,
            recordedByUserId);
}
