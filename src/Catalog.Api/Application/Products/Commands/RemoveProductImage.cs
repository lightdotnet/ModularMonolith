using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record RemoveProductImageCommand(long Id, string Url) : ICommand<IResult>;

internal sealed class RemoveProductImageCommandValidator : AbstractValidator<RemoveProductImageCommand>
{
    public RemoveProductImageCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Url).NotEmpty();
    }
}

internal class RemoveProductImageCommandHandler(CatalogDbContext context)
    : ICommandHandler<RemoveProductImageCommand, IResult>
{
    public async Task<IResult> Handle(
        RemoveProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        entity.RemoveImage(request.Url);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
