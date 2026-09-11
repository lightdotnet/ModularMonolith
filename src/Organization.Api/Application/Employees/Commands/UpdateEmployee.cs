using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record UpdateEmployeeCommand(EmployeeDto Model) : ICommand<IResult>;

internal class UpdateEmployeeCommandHandler(OrganizationDbContext context)
    : ICommandHandler<UpdateEmployeeCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.Employees
            .Where(new EmployeeByIdSpec(model.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Employee {model.Id} not found");

        var codeTaken = await context.Employees
            .AnyAsync(x => x.Id != model.Id && x.CompanyId == entity.CompanyId && x.EmployeeCode == model.EmployeeCode, cancellationToken);

        if (codeTaken)
            return Result.Error($"Employee code '{model.EmployeeCode}' already exists in this company.");

        entity.EmployeeCode = model.EmployeeCode;
        entity.FirstName = model.FirstName;
        entity.LastName = model.LastName;
        entity.DateOfBirth = model.DateOfBirth;
        entity.Gender = model.Gender;
        entity.NationalId = model.NationalId;
        entity.Email = model.Email;
        entity.PhoneNumber = model.PhoneNumber;
        entity.Address = model.Address;
        entity.HireDate = model.HireDate;
        entity.TerminationDate = model.TerminationDate;
        entity.EmploymentStatus = model.EmploymentStatus;
        entity.AvatarUrl = model.AvatarUrl;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
