using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.Suppliers;

namespace StarterKit.Purchasing.Api.Application.Suppliers.Commands;

/// <summary>Activates or deactivates a supplier; a deactivated supplier cannot be put on new purchase orders.</summary>
internal sealed record ChangeSupplierStatusCommand(
    long Id,
    bool Activate) : ICommand<IResult>;

internal sealed class ChangeSupplierStatusCommandValidator : AbstractValidator<ChangeSupplierStatusCommand>
{
    public ChangeSupplierStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal class ChangeSupplierStatusCommandHandler(PurchasingDbContext context)
    : ICommandHandler<ChangeSupplierStatusCommand, IResult>
{
    public async Task<IResult> Handle(
        ChangeSupplierStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Suppliers
            .Where(new SupplierByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Supplier {request.Id} not found");

        if (request.Activate)
            entity.Activate();
        else
            entity.Deactivate();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
