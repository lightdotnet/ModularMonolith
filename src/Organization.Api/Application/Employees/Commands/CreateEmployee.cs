using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record CreateEmployeeCommand(CreateEmployeeRequest Model) : ICommand<IResult<string>>;

internal class CreateEmployeeCommandHandler(OrganizationDbContext context)
    : ICommandHandler<CreateEmployeeCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var companyExists = await context.Companies.AnyAsync(x => x.Id == model.CompanyId, cancellationToken);

        if (!companyExists)
            return Result<string>.NotFound($"Company {model.CompanyId} not found");

        var codeExists = await context.Employees
            .AnyAsync(x => x.CompanyId == model.CompanyId && x.EmployeeCode == model.EmployeeCode, cancellationToken);

        if (codeExists)
            return Result<string>.Error($"Employee code '{model.EmployeeCode}' already exists in this company.");

        var entity = new Employee
        {
            CompanyId = model.CompanyId,
            EmployeeCode = model.EmployeeCode,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            NationalId = model.NationalId,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            HireDate = model.HireDate,
            AvatarUrl = model.AvatarUrl,
        };

        await context.Employees.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success(entity.Id);
    }
}
