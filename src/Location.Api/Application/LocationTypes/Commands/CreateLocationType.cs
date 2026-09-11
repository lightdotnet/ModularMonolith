using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;

namespace StarterKit.Locations.Api.Application.LocationTypes.Commands;

internal sealed record CreateLocationTypeCommand(CreateLocationTypeRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateLocationTypeCommandValidator : AbstractValidator<CreateLocationTypeCommand>
{
    public CreateLocationTypeCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateLocationTypeRequestValidator());
    }
}

internal class CreateLocationTypeCommandHandler(
    LocationDbContext context,
    ILocationTypeCache locationTypeCache)
    : ICommandHandler<CreateLocationTypeCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateLocationTypeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var idExists = await context.LocationTypes
            .AnyAsync(x => x.Id == model.Id, cancellationToken);

        if (idExists)
            return Result<string>.Error($"Location type '{model.Id}' already exists.");

        if (!string.IsNullOrEmpty(model.AllowedParentTypeId))
        {
            var parentTypeExists = await context.LocationTypes
                .AnyAsync(x => x.Id == model.AllowedParentTypeId, cancellationToken);

            if (!parentTypeExists)
                return Result<string>.NotFound($"Location type {model.AllowedParentTypeId} not found");
        }

        var entity = LocationType.Create(
            model.Id,
            model.Name,
            model.AllowedParentTypeId,
            model.CanHaveChildren);

        await context.LocationTypes.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await locationTypeCache.ReloadAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
