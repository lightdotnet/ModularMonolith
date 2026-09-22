using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record ActivateProductCommand(long Id) : ICommand<IResult>;

internal sealed class ActivateProductCommandValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal class ActivateProductCommandHandler(CatalogDbContext context)
    : ICommandHandler<ActivateProductCommand, IResult>
{
    public async Task<IResult> Handle(
        ActivateProductCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        entity.Activate();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
