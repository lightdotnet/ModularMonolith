using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.Orders;

public record ApplyOrderDiscountRequest
{
    public OrderDiscountKind Kind { get; set; }

    public decimal Value { get; set; }
}

public sealed class ApplyOrderDiscountRequestValidator : AbstractValidator<ApplyOrderDiscountRequest>
{
    public ApplyOrderDiscountRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.Kind == OrderDiscountKind.Percentage);
    }
}
