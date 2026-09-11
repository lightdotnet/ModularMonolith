using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record DeleteEmployeeCommand(string Id) : ICommand<IResult>;

internal class DeleteEmployeeCommandHandler(OrganizationDbContext context)
    : ICommandHandler<DeleteEmployeeCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.Employees
            .Where(new EmployeeByIdSpec(request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.NotFound($"Employee {request.Id} not found");

        context.Employees.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
