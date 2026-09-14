namespace StarterKit.Orders.Api.Domain.Orders;

public class OrderByIdSpec : Specification<Order>
{
    public OrderByIdSpec(long orderId)
    {
        Where(x => x.Id == orderId);
    }
}
