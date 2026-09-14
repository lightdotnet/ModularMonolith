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
        OrderFeeType type)
    {
        OrderId = orderId;
        OrderCode = orderCode;
        Name = name;
        Amount = amount;
        Type = type;
    }

    public long OrderId { get; private set; }

    /// <summary>Denormalized snapshot of the parent <c>Order.OrderCode.Value</c> taken at <see cref="Create"/> time — same treatment as <c>OrderLine.OrderCode</c>.</summary>
    public string OrderCode { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public Money Amount { get; private set; } = null!;

    public OrderFeeType Type { get; private set; }

    public virtual Order Order { get; private set; } = null!;

    internal static OrderFee Create(
        long orderId,
        string orderCode,
        string name,
        Money amount,
        OrderFeeType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw Invalid(nameof(name), "A fee name is required.");

        ArgumentNullException.ThrowIfNull(amount);

        return new OrderFee(orderId, orderCode, name, amount, type);
    }

    private static ValidationException Invalid(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
