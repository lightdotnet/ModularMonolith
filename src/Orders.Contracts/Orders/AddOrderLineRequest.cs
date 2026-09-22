namespace StarterKit.Orders.Contracts.Orders;

public record AddOrderLineRequest
{
    public long ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal? RequestedSalePrice { get; set; }
}

public sealed class AddOrderLineRequestValidator : AbstractValidator<AddOrderLineRequest>
{
    public AddOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.RequestedSalePrice).GreaterThanOrEqualTo(0).When(x => x.RequestedSalePrice.HasValue);
    }
}
