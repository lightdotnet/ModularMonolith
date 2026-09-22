using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.LocationTypes;
using StarterKit.Locations.Api.Services;

namespace StarterKit.Locations.Api.Application.LocationTypes.Commands;

internal sealed record UpdateLocationTypeCommand(string Id, UpdateLocationTypeRequest Model) : ICommand<IResult>;

internal sealed class UpdateLocationTypeCommandValidator : AbstractValidator<UpdateLocationTypeCommand>
{
    public UpdateLocationTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new UpdateLocationTypeRequestValidator());
    }
}

internal class UpdateLocationTypeCommandHandler(
    LocationDbContext context,
    ILocationTypeCache locationTypeCache)
    : ICommandHandler<UpdateLocationTypeCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateLocationTypeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.LocationTypes
            .Where(new LocationTypeByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Location type {request.Id} not found");

        if (!string.IsNullOrEmpty(model.AllowedParentTypeId))
        {
            if (model.AllowedParentTypeId == request.Id)
                return Result.Error("A location type cannot be its own allowed parent type.");

            var parentTypeExists = await context.LocationTypes
                .AnyAsync(x => x.Id == model.AllowedParentTypeId, cancellationToken);

            if (!parentTypeExists)
                return Result.NotFound($"Location type {model.AllowedParentTypeId} not found");
        }

        entity.Update(model.Name, model.AllowedParentTypeId, model.CanHaveChildren, model.Status);

        await context.SaveChangesAsync(cancellationToken);

        await locationTypeCache.ReloadAsync(cancellationToken);

        return Result.Success();
    }
}
