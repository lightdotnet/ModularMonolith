using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.Suppliers;

namespace StarterKit.Purchasing.Api.Application.Suppliers.Commands;

internal sealed record CreateSupplierCommand(CreateSupplierRequest Model) : ICommand<IResult<long>>;

internal sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Model).SetValidator(new CreateSupplierRequestValidator());
    }
}

internal class CreateSupplierCommandHandler(PurchasingDbContext context)
    : ICommandHandler<CreateSupplierCommand, IResult<long>>
{
    public async Task<IResult<long>> Handle(
        CreateSupplierCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var code = Supplier.NormalizeCode(model.Code);

        if (await context.Suppliers.AnyAsync(x => x.Code == code, cancellationToken))
            return Result<long>.Error($"Supplier code '{code}' already exists.");

        var entity = Supplier.Create(
            model.Code,
            model.Name,
            model.ContactName,
            model.Phone,
            model.Email,
            model.Address,
            model.PaymentTerms);

        await context.Suppliers.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(entity.Id);
    }
}
