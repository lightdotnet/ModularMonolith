using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Companies;

namespace StarterKit.Organization.Api.Application.Companies.Commands;

internal sealed record DeleteCompanyCommand(string Id) : ICommand<IResult>;

internal class DeleteCompanyCommandHandler(OrganizationDbContext context)
    : ICommandHandler<DeleteCompanyCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Companies
            .Where(new CompanyByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Company {request.Id} not found");

        var hasOrgUnits = await context.OrgUnits.AnyAsync(x => x.CompanyId == request.Id, cancellationToken);

        if (hasOrgUnits)
            return Result.Error("Company still has departments/teams. Remove them first.");

        var hasEmployees = await context.Employees.AnyAsync(x => x.CompanyId == request.Id, cancellationToken);

        if (hasEmployees)
            return Result.Error("Company still has employees. Remove them first.");

        context.Companies.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
