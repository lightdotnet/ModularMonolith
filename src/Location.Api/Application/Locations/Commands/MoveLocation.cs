using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.Locations;
using StarterKit.Locations.Api.Services;

namespace StarterKit.Locations.Api.Application.Locations.Commands;

internal sealed record MoveLocationCommand(string Id, MoveLocationRequest Model) : ICommand<IResult>;

internal sealed class MoveLocationCommandValidator : AbstractValidator<MoveLocationCommand>
{
    public MoveLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new MoveLocationRequestValidator());
    }
}

internal class MoveLocationCommandHandler(
    LocationDbContext context,
    ILocationTypeCache locationTypeCache)
    : ICommandHandler<MoveLocationCommand, IResult>
{
    public async Task<IResult> Handle(
        MoveLocationCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Locations
            .Where(new LocationByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Location {request.Id} not found");

        var type = await locationTypeCache.GetAsync(entity.LocationTypeId, cancellationToken);

        if (type is null)
            return Result.NotFound($"Location type {entity.LocationTypeId} not found");

        var newParentId = request.Model.NewParentLocationId;

        if (string.IsNullOrEmpty(newParentId))
        {
            entity.Move(type, null, null);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        if (newParentId == entity.Id)
            return Result.Error("A location cannot be its own parent.");

        var newParent = await context.Locations
            .Include(x => x.Type)
            .Where(new LocationByIdSpec(newParentId))
            .FirstOrDefaultAsync(cancellationToken);

        if (newParent is null)
            return Result.NotFound($"Location {newParentId} not found");

        var isDescendant = await IsDescendantAsync(context, entity.Id, newParent.Id, cancellationToken);

        if (isDescendant)
            return Result.Error("Cannot move a location under one of its own descendants.");

        entity.Move(type, newParentId, newParent);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static async Task<bool> IsDescendantAsync(
        LocationDbContext context, string ancestorId, string candidateId, CancellationToken cancellationToken)
    {
        var currentId = candidateId;

        while (!string.IsNullOrEmpty(currentId))
        {
            if (currentId == ancestorId)
                return true;

            currentId = await context.Locations
                .Where(new LocationByIdSpec(currentId))
                .Select(x => x.ParentLocationId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return false;
    }
}
