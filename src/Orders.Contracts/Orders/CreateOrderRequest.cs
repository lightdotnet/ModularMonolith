namespace StarterKit.Orders.Contracts.Orders;

public record CreateOrderRequest
{
    public string LocationId { get; set; } = null!;

    public string? MemberId { get; set; }

    /// <summary>
    /// Optional caller-supplied human-readable order reference. Omitted (<c>null</c>/blank) falls
    /// back to a system-generated <c>yyyyMMdd</c> + 9-char code; a supplied value must be unique
    /// (rejected as a conflict if already taken) and is never auto-regenerated on a collision.
    /// </summary>
    public string? OrderCode { get; set; }

    /// <summary>Opaque reference back to an order in an external system (POS/marketplace/etc.); no format/uniqueness rule of its own.</summary>
    public string? ExternalReferenceCode { get; set; }
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.LocationId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.MemberId).MaximumLength(450);
        RuleFor(x => x.OrderCode).MaximumLength(17).When(x => x.OrderCode is not null);
        RuleFor(x => x.ExternalReferenceCode).MaximumLength(50);
    }
}
