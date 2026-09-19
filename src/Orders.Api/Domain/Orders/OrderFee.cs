using StarterKit.Shared.Entities;
using StarterKit.Shared.ValueObjects;
using ValidationException = Light.Exceptions.ValidationException;

namespace StarterKit.Orders.Api.Domain.Orders;

/// <summary>
/// A single extra charge on an <see cref="Orders.Order"/> (shipping, etc.), own <c>Id</c>,
/// individually removable — same shape as <see cref="OrderLine"/>.
/// </summary>
public class OrderFee : AuditableEntity<long>
{
    private OrderFee()
    {
    }

    private OrderFee(
        long orderId,
        string orderCode,
        string name,
        Money amount,
        string feeTypeId,
        string feeTypeName)
    {
        OrderId = orderId;
        OrderCode = orderCode;
        Name = name;
        Amount = amount;
        FeeTypeId = feeTypeId;
        FeeTypeName = feeTypeName;
    }

    public long OrderId { get; private set; }

    /// <summary>Denormalized snapshot of the parent <c>Order.OrderCode.Value</c> taken at <see cref="Create"/> time — same treatment as <c>OrderLine.OrderCode</c>.</summary>
    public string OrderCode { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public Money Amount { get; private set; } = null!;

    /// <summary>Plain denormalized id of the <c>FeeTypes</c> catalog entry chosen at <see cref="Create"/> time — no FK, no navigation, same treatment as <see cref="OrderCode"/>.</summary>
    public string FeeTypeId { get; private set; } = null!;

    /// <summary>Denormalized snapshot of <c>FeeType.Name</c> taken at <see cref="Create"/> time — same treatment as <see cref="OrderCode"/>.</summary>
    public string FeeTypeName { get; private set; } = null!;

    public virtual Order Order { get; private set; } = null!;

    internal static OrderFee Create(
        long orderId,
        string orderCode,
        string name,
        Money amount,
        string feeTypeId,
        string feeTypeName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw Invalid(nameof(name), "A fee name is required.");

        if (string.IsNullOrWhiteSpace(feeTypeId))
            throw Invalid(nameof(feeTypeId), "A fee type is required.");

        if (string.IsNullOrWhiteSpace(feeTypeName))
            throw Invalid(nameof(feeTypeName), "A fee type name is required.");

        ArgumentNullException.ThrowIfNull(amount);

        return new OrderFee(orderId, orderCode, name, amount, feeTypeId, feeTypeName);
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
