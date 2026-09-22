namespace StarterKit.Orders.Contracts.Orders;

public record UpdateOrderLineQuantityRequest
{
    public int Quantity { get; set; }
}

public sealed class UpdateOrderLineQuantityRequestValidator : AbstractValidator<UpdateOrderLineQuantityRequest>
{
    public UpdateOrderLineQuantityRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
