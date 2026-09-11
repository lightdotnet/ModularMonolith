using StarterKit.Identity.Contracts.Services;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Shared.Constants;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record UnlinkEmployeeLoginCommand(string EmployeeId) : ICommand<IResult>;

internal class UnlinkEmployeeLoginCommandHandler(OrganizationDbContext context, IUserService userService)
    : ICommandHandler<UnlinkEmployeeLoginCommand, IResult>
{
    public async Task<IResult> Handle(
        UnlinkEmployeeLoginCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await context.Employees
            .Where(new EmployeeByIdSpec(request.EmployeeId))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
            return Result.NotFound($"Employee {request.EmployeeId} not found");

        var userId = employee.UserId;

        employee.UserId = null;

        await context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(userId))
            await userService.SetClaimAsync(userId, ClaimTypeConstants.EmployeeId, null);

        return Result.Success();
    }
}
