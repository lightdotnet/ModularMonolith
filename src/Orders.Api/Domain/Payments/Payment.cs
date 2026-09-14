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
        PaymentMethod method,
        DateTimeOffset paidAt,
        string? reference,
        string recordedByUserId)
    {
        OrderId = orderId;
        OrderCode = orderCode;
        Amount = amount;
        Method = method;
        PaidAt = paidAt;
        Reference = reference;
        RecordedByUserId = recordedByUserId;
    }

    public long OrderId { get; private set; }

    /// <summary>Denormalized snapshot of the parent <c>Order.OrderCode.Value</c> taken at <see cref="Create"/> time — same treatment as <c>OrderLine.OrderCode</c>.</summary>
    public string OrderCode { get; private set; } = null!;

    public Money Amount { get; private set; } = null!;

    public PaymentMethod Method { get; private set; }

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
        PaymentMethod method,
        DateTimeOffset paidAt,
        string? reference,
        string recordedByUserId)
    {
        if (orderId <= 0)
            throw Invalid(nameof(orderId), "An order is required.");

        if (string.IsNullOrWhiteSpace(recordedByUserId))
            throw Invalid(nameof(recordedByUserId), "A recording user is required.");

        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
            throw Invalid(nameof(amount), "Payment amount must be greater than zero.");

        return new Payment(
            orderId,
            orderCode,
            amount,
            method,
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
