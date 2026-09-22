using Light.Exceptions;
using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Orders.Api.Domain.Payments;

/// <summary>
/// A single payment recorded against an order — its own aggregate root, sharing the
/// <c>OrdersDbContext</c>/database with <c>Orders.Domain.Orders.Order</c> but holding only the
/// opaque <see cref="OrderId"/>, no navigation back to it: the aggregate boundary is deliberate, so
/// recording a payment never loads or locks the entire order graph. Voiding a payment does not
/// delete it — the audit trail (who recorded it, who voided it, why) is kept.
/// </summary>
public class Payment : AuditableEntity<long>
{
    private Payment()
    {
    }

    private Payment(
        long orderId,
        string orderCode,
        Money amount,
        string paymentTypeId,
        string paymentTypeName,
        DateTimeOffset paidAt,
        string? reference,
        string recordedByUserId)
    {
        OrderId = orderId;
        OrderCode = orderCode;
        Amount = amount;
        PaymentTypeId = paymentTypeId;
        PaymentTypeName = paymentTypeName;
        PaidAt = paidAt;
        Reference = reference;
        RecordedByUserId = recordedByUserId;
    }

    public long OrderId { get; private set; }

    /// <summary>Denormalized snapshot of the parent <c>Order.OrderCode.Value</c> taken at <see cref="Create"/> time — same treatment as <c>OrderLine.OrderCode</c>.</summary>
    public string OrderCode { get; private set; } = null!;

    public Money Amount { get; private set; } = null!;

    /// <summary>Plain denormalized id of the <c>PaymentTypes</c> catalog entry chosen at <see cref="Create"/> time — no FK, no navigation, same treatment as <see cref="OrderCode"/>.</summary>
    public string PaymentTypeId { get; private set; } = null!;

    /// <summary>Denormalized snapshot of <c>PaymentType.Name</c> taken at <see cref="Create"/> time — same treatment as <see cref="OrderCode"/>.</summary>
    public string PaymentTypeName { get; private set; } = null!;

    public DateTimeOffset PaidAt { get; private set; }

    public string? Reference { get; private set; }

    public string RecordedByUserId { get; private set; } = null!;

    public bool IsVoided { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public string? VoidReason { get; private set; }

    public static Payment Create(
        long orderId,
        string orderCode,
        Money amount,
        string orderCurrencyCode,
        string paymentTypeId,
        string paymentTypeName,
        DateTimeOffset paidAt,
        string? reference,
        string recordedByUserId)
    {
        if (orderId <= 0)
            throw Invalid(nameof(orderId), "An order is required.");

        if (string.IsNullOrWhiteSpace(recordedByUserId))
            throw Invalid(nameof(recordedByUserId), "A recording user is required.");

        if (string.IsNullOrWhiteSpace(paymentTypeId))
            throw Invalid(nameof(paymentTypeId), "A payment type is required.");

        if (string.IsNullOrWhiteSpace(paymentTypeName))
            throw Invalid(nameof(paymentTypeName), "A payment type name is required.");

        ArgumentNullException.ThrowIfNull(amount);

        // A payment is its own aggregate and holds no reference to the order, so the caller supplies
        // the order's currency; a payment is always settled in exactly that currency.
        if (amount.Currency != orderCurrencyCode)
            throw Invalid(nameof(amount), $"Payment must be in the order currency {orderCurrencyCode}.");

        if (amount.Amount <= 0)
            throw Invalid(nameof(amount), "Payment amount must be greater than zero.");

        return new Payment(
            orderId,
            orderCode,
            amount,
            paymentTypeId,
            paymentTypeName,
            paidAt,
            reference,
            recordedByUserId);
    }

    public void Void(
        string reason,
        DateTimeOffset voidedAt)
    {
        if (IsVoided)
            throw new ConflictException("This payment has already been voided.");

        if (string.IsNullOrWhiteSpace(reason))
            throw Invalid(nameof(reason), "A void reason is required.");

        IsVoided = true;
        VoidedAt = voidedAt;
        VoidReason = reason;
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
