using StarterKit.Purchasing.Api.Data;
using StarterKit.Purchasing.Api.Domain.Suppliers;

namespace StarterKit.Purchasing.Api.Application.Suppliers.Queries;

internal sealed record GetSupplierByIdQuery(long Id) : IQuery<IResult<SupplierDto>>;

internal class GetSupplierByIdQueryHandler(PurchasingDbContext context)
    : IQueryHandler<GetSupplierByIdQuery, IResult<SupplierDto>>
{
    public async Task<IResult<SupplierDto>> Handle(
        GetSupplierByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Suppliers
            .AsNoTracking()
            .Where(new SupplierByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result<SupplierDto>.NotFound($"Supplier {request.Id} not found");

        return Result<SupplierDto>.Success(ToDto(entity));
    }

    internal static SupplierDto ToDto(Supplier entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        ContactName = entity.ContactName,
        Phone = entity.Phone,
        Email = entity.Email,
        Address = entity.Address,
        PaymentTerms = entity.PaymentTerms,
        Status = entity.Status,
    };
}
