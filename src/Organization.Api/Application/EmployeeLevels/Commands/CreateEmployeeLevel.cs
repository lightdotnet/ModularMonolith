using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;

namespace StarterKit.Organization.Api.Application.EmployeeLevels.Commands;

internal sealed record CreateEmployeeLevelCommand(CreateEmployeeLevelRequest Model) : ICommand<IResult<string>>;

internal class CreateEmployeeLevelCommandHandler(OrganizationDbContext context)
    : ICommandHandler<CreateEmployeeLevelCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateEmployeeLevelCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var companyExists = await context.Companies.AnyAsync(x => x.Id == model.CompanyId, cancellationToken);

        if (!companyExists)
            return Result<string>.NotFound($"Company {model.CompanyId} not found");

        var codeExists = await context.EmployeeLevels
            .AnyAsync(x => x.CompanyId == model.CompanyId && x.Code == model.Code, cancellationToken);

        if (codeExists)
            return Result<string>.Error($"Employee level code '{model.Code}' already exists in this company.");

        var entity = new EmployeeLevel
        {
            CompanyId = model.CompanyId,
            Name = model.Name,
            Code = model.Code,
            Rank = model.Rank,
            Description = model.Description,
        };

        await context.EmployeeLevels.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
