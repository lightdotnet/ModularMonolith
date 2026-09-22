using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.Suppliers;

namespace StarterKit.Purchasing.Api.Application.Suppliers.Commands;

internal sealed record UpdateSupplierCommand(
    long Id,
    UpdateSupplierRequest Model) : ICommand<IResult>;

internal sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Model).SetValidator(new UpdateSupplierRequestValidator());
    }
}

internal class UpdateSupplierCommandHandler(PurchasingDbContext context)
    : ICommandHandler<UpdateSupplierCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateSupplierCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Suppliers
            .Where(new SupplierByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Supplier {request.Id} not found");

        var model = request.Model;

        var code = Supplier.NormalizeCode(model.Code);

        if (await context.Suppliers.AnyAsync(x => x.Code == code && x.Id != entity.Id, cancellationToken))
            return Result.Error($"Supplier code '{code}' already exists.");

        entity.Update(
            model.Code,
            model.Name,
            model.ContactName,
            model.Phone,
            model.Email,
            model.Address,
            model.PaymentTerms);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
