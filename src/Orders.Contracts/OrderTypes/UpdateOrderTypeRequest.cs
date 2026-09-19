using StarterKit.Orders.Contracts.Common;

namespace StarterKit.Orders.Contracts.OrderTypes;

public record UpdateOrderTypeRequest
{
    public string Name { get; set; } = null!;

    public OrderTypeStatus Status { get; set; }
}

public sealed class UpdateOrderTypeRequestValidator : AbstractValidator<UpdateOrderTypeRequest>
{
    public UpdateOrderTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum();
    }
}
