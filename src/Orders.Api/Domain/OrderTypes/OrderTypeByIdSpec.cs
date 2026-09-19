namespace StarterKit.Orders.Api.Domain.OrderTypes;

/// <summary>Looks up a row by the composite <c>(Id, Category)</c> key — replaces the separate
/// <c>FeeTypeByIdSpec</c>/<c>PaymentTypeByIdSpec</c>, both of which only needed <c>Id</c>.</summary>
public class OrderTypeByIdSpec : Specification<OrderType>
{
    public OrderTypeByIdSpec(
        string id,
        OrderTypeCategory category)
    {
        Where(x => x.Id == id && x.Category == category);
    }
}
