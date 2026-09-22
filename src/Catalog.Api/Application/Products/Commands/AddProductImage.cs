using StarterKit.Catalog.Api.Data;
using StarterKit.Catalog.Api.Domain.Products;

namespace StarterKit.Catalog.Api.Application.Products.Commands;

internal sealed record AddProductImageCommand(long Id, AddProductImageRequest Model) : ICommand<IResult>;

internal sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new AddProductImageRequestValidator());
    }
}

internal class AddProductImageCommandHandler(CatalogDbContext context)
    : ICommandHandler<AddProductImageCommand, IResult>
{
    public async Task<IResult> Handle(
        AddProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .Where(new ProductByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Product {request.Id} not found");

        entity.AddImage(new ProductImageUrl(request.Model.Url, request.Model.SortOrder));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
