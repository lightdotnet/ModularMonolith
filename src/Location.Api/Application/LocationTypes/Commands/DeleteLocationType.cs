using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;

namespace StarterKit.Locations.Api.Application.LocationTypes.Commands;

internal sealed record DeleteLocationTypeCommand(string Id) : ICommand<IResult>;

internal sealed class DeleteLocationTypeCommandValidator : AbstractValidator<DeleteLocationTypeCommand>
{
    public DeleteLocationTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal class DeleteLocationTypeCommandHandler(
    LocationDbContext context,
    ILocationTypeCache locationTypeCache)
    : ICommandHandler<DeleteLocationTypeCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteLocationTypeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.LocationTypes
            .Where(new LocationTypeByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Location type {request.Id} not found");

        var inUseByLocations = await context.Locations
            .AnyAsync(x => x.LocationTypeId == request.Id, cancellationToken);

        if (inUseByLocations)
            return Result.Error("Location type is still assigned to one or more locations.");

        var referencedByOtherTypes = await context.LocationTypes
            .AnyAsync(x => x.AllowedParentTypeId == request.Id, cancellationToken);

        if (referencedByOtherTypes)
            return Result.Error("Location type is still referenced as the allowed parent type of another location type.");

        context.LocationTypes.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        await locationTypeCache.ReloadAsync(cancellationToken);

        return Result.Success();
    }
}
