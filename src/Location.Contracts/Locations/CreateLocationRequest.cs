namespace StarterKit.Locations.Contracts.Locations;

public record CreateLocationRequest
{
    public string? ParentLocationId { get; set; }

    public string LocationTypeId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;
}

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.ParentLocationId).MaximumLength(450);
        RuleFor(x => x.LocationTypeId).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}
