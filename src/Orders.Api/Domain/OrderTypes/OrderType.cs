using StarterKit.Shared.Entities;

namespace StarterKit.Orders.Api.Domain.OrderTypes;

/// <summary>
/// Unified data-driven catalog replacing the former separate <c>FeeType</c>/<c>PaymentType</c>
/// catalogs (each its own table/controller/permissions/cache). <see cref="Category"/> discriminates
/// a fee type from a payment type; both catalogs seed a colliding code (<c>"OTHER"</c>), so identity
/// is the composite <c>(Id, Category)</c> pair rather than <see cref="Id"/> alone — see
/// <see cref="OrderTypeByIdSpec"/>. <see cref="Orders.OrderFee"/>/<see cref="Payments.Payment"/> hold
/// their own <c>*TypeId</c>/<c>*TypeName</c> snapshot, with no FK back to this table; a caller
/// resolving one of those ids always passes the category it expects (see
/// <c>Services.IOrderTypeCache.GetAsync</c>), which is itself the guard against e.g. a
/// Payment-category id being submitted as a fee type. Flat catalog, no hierarchy. Unlike most
/// aggregates in this solution, <see cref="Id"/> is a caller-supplied business code (e.g.
/// <c>"SHIPPING"</c>), not a framework-generated id — see <see cref="Create"/>.
/// </summary>
public class OrderType : AuditableEntity
{
    private OrderType()
    {
    }

    public OrderTypeCategory Category { get; private set; }

    public string Name { get; private set; } = null!;

    public OrderTypeStatus Status { get; private set; } = OrderTypeStatus.Active;

    /// <summary>
    /// Creates a new order type with a caller-supplied <paramref name="id"/>, unique only within
    /// <paramref name="category"/> — not globally. Whether that (id, category) pair is already taken
    /// needs a database walk and is the caller's responsibility (see
    /// <c>CreateOrderTypeCommandHandler</c>). Field-shape validation (required/length) is
    /// FluentValidation's job on the incoming request, not this factory's — only real domain rules
    /// live here. <paramref name="category"/> is immutable after creation — see <see cref="Update"/>.
    /// </summary>
    public static OrderType Create(
        string id,
        OrderTypeCategory category,
        string name)
    {
        return new OrderType
        {
            Id = id,
            Category = category,
            Name = name,
            Status = OrderTypeStatus.Active,
        };
    }

    public void Update(
        string name,
        OrderTypeStatus status)
    {
        Name = name;
        Status = status;
    }
}
