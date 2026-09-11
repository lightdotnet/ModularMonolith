using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.Locations;

namespace StarterKit.Locations.Api.Application.Locations.Commands;

internal sealed record UpdateLocationCommand(string Id, UpdateLocationRequest Model) : ICommand<IResult>;

internal sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Model).SetValidator(new UpdateLocationRequestValidator());
    }
}

internal class UpdateLocationCommandHandler(LocationDbContext context)
    : ICommandHandler<UpdateLocationCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.Locations
            .Where(new LocationByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Location {request.Id} not found");

        var codeTaken = await context.Locations
            .AnyAsync(x => x.Id != request.Id && x.Code == model.Code, cancellationToken);

        if (codeTaken)
            return Result.Error($"Location code '{model.Code}' already exists.");

        entity.Update(model.Name, model.Code, model.Status);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
