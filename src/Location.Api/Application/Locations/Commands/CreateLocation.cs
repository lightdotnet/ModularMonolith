using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Domain.Locations;
using StarterKit.Locations.Api.Services;

namespace StarterKit.Locations.Api.Application.Locations.Commands;

internal sealed record CreateLocationCommand(CreateLocationRequest Model) : ICommand<IResult<string>>;

internal sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateLocationRequestValidator());
    }
}

internal class CreateLocationCommandHandler(
    LocationDbContext context,
    ILocationTypeCache locationTypeCache)
    : ICommandHandler<CreateLocationCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateLocationCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var type = await locationTypeCache.GetAsync(model.LocationTypeId, cancellationToken);

        if (type is null)
            return Result<string>.NotFound($"Location type {model.LocationTypeId} not found");

        Location? parent = null;

        if (!string.IsNullOrEmpty(model.ParentLocationId))
        {
            parent = await context.Locations
                .Include(x => x.Type)
                .Where(new LocationByIdSpec(model.ParentLocationId))
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
                return Result<string>.NotFound($"Location {model.ParentLocationId} not found");
        }

        var codeExists = await context.Locations
            .AnyAsync(x => x.Code == model.Code, cancellationToken);

        if (codeExists)
            return Result<string>.Error($"Location code '{model.Code}' already exists.");

        var trackedType = context.ChangeTracker.Entries<LocationType>()
            .FirstOrDefault(e => e.Entity.Id == type.Id)?.Entity;

        if (trackedType is not null)
            type = trackedType;
        else
            context.Attach(type);

        var entity = Location.Create(
            model.Name,
            model.Code,
            type,
            model.ParentLocationId,
            parent);

        await context.Locations.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
