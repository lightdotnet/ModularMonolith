using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record DeactivateProductCommand(string Id) : ICommand<IResult>;

internal sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal class DeactivateProductCommandHandler(CatalogDbContext context)
    : ICommandHandler<DeactivateProductCommand, IResult>
{
    public async Task<IResult> Handle(
        DeactivateProductCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        entity.Deactivate();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
