namespace StarterKit.Orders.Contracts.Orders;

public record SetOrderLineSalePriceRequest
{
    public decimal? SalePrice { get; set; }
}

public sealed class SetOrderLineSalePriceRequestValidator : AbstractValidator<SetOrderLineSalePriceRequest>
{
    public SetOrderLineSalePriceRequestValidator()
    {
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
    }
}
