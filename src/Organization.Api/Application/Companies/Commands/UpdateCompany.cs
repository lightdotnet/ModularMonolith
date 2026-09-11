using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Companies;

namespace StarterKit.Organization.Api.Application.Companies.Commands;

internal sealed record UpdateCompanyCommand(CompanyDto Model) : ICommand<IResult>;

internal class UpdateCompanyCommandHandler(OrganizationDbContext context)
    : ICommandHandler<UpdateCompanyCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.Companies
            .Where(new CompanyByIdSpec(request.Model.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Company {model.Id} not found");

        var codeTaken = await context.Companies
            .AnyAsync(x => x.Id != model.Id && x.Code == model.Code, cancellationToken);

        if (codeTaken)
            return Result.Error($"Company code '{model.Code}' already exists.");

        entity.Name = model.Name;
        entity.Code = model.Code;
        entity.TaxCode = model.TaxCode;
        entity.Address = model.Address;
        entity.Phone = model.Phone;
        entity.Email = model.Email;
        entity.Website = model.Website;
        entity.Description = model.Description;
        entity.Status = model.Status;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
