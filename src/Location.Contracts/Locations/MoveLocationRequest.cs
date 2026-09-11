namespace StarterKit.Locations.Contracts.Locations;

public record MoveLocationRequest
{
    public string? NewParentLocationId { get; set; }
}

public sealed class MoveLocationRequestValidator : AbstractValidator<MoveLocationRequest>
{
    public MoveLocationRequestValidator()
    {
        RuleFor(x => x.NewParentLocationId).MaximumLength(450);
    }
}
