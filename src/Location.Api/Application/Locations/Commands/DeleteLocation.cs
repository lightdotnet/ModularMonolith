using StarterKit.Locations.Api.Data;
using StarterKit.Locations.Api.Domain.Locations;

namespace StarterKit.Locations.Api.Application.Locations.Commands;

internal sealed record DeleteLocationCommand(string Id) : ICommand<IResult>;

internal sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal class DeleteLocationCommandHandler(LocationDbContext context)
    : ICommandHandler<DeleteLocationCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteLocationCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Locations
            .Where(new LocationByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Location {request.Id} not found");

        var hasChildren = await context.Locations
            .AnyAsync(x => x.ParentLocationId == request.Id, cancellationToken);

        if (hasChildren)
            return Result.Error("Location still has child locations. Remove them first.");

        context.Locations.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
