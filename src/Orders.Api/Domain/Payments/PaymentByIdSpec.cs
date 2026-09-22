namespace StarterKit.Orders.Api.Domain.Payments;

public class PaymentByIdSpec : Specification<Payment>
{
    public PaymentByIdSpec(long paymentId)
    {
        Where(x => x.Id == paymentId);
    }
}
