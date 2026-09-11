namespace StarterKit.Locations.Contracts.LocationTypes;

public record CreateLocationTypeRequest
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? AllowedParentTypeId { get; set; }

    public bool CanHaveChildren { get; set; }
}

public sealed class CreateLocationTypeRequestValidator : AbstractValidator<CreateLocationTypeRequest>
{
    public CreateLocationTypeRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(450);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AllowedParentTypeId).MaximumLength(450);
    }
}
