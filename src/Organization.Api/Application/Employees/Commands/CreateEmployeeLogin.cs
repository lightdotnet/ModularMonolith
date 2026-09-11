using StarterKit.Identity.Contracts;
using StarterKit.Identity.Contracts.Services;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Shared.Constants;

namespace StarterKit.Organization.Api.Application.Employees.Commands;

internal sealed record CreateEmployeeLoginCommand(string EmployeeId, CreateEmployeeLoginRequest Model)
    : ICommand<IResult<string>>;

internal class CreateEmployeeLoginCommandHandler(OrganizationDbContext context, IUserService userService)
    : ICommandHandler<CreateEmployeeLoginCommand, IResult<string>>
{
    public async Task<IResult<string>> Handle(
        CreateEmployeeLoginCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await context.Employees
            .Where(new EmployeeByIdSpec(request.EmployeeId))
            .FirstOrDefaultAsync(cancellationToken);

        if (employee is null)
            return Result<string>.NotFound($"Employee {request.EmployeeId} not found");

        if (!string.IsNullOrEmpty(employee.UserId))
            return Result<string>.Error("Employee already has a login account.");

        var model = request.Model;

        var userResult = await userService.CreateAsync(new CreateUserRequest
        {
            UserName = model.UserName,
            Password = model.Password,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
        });

        if (!userResult.IsSuccess)
            return userResult;

        // Claim the link with a conditional UPDATE (WHERE UserId IS NULL) so two concurrent
        // CreateEmployeeLogin calls for the same employee cannot both succeed and strand one of
        // the freshly minted Identity users. A plain SaveChangesAsync would let the second UPDATE
        // silently overwrite the first.
        var linked = await context.Employees
            .Where(x => x.Id == request.EmployeeId && x.UserId == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UserId, userResult.Data),
                cancellationToken);

        if (linked == 0)
        {
            // Another request won the race (or a link appeared after the check above) - roll back
            // the user we just created so it does not linger without an employee.
            await userService.DeleteAsync(userResult.Data);
            return Result<string>.Error("Employee already has a login account.");
        }

        try
        {
            await userService.SetClaimAsync(userResult.Data, ClaimTypeConstants.EmployeeId, employee.Id);
        }
        catch
        {
            // Undo the whole operation: release the link we just took and drop the claim owner.
            // Runs regardless of why we failed, so it must not observe the request's cancellation.
            await context.Employees
                .Where(x => x.Id == request.EmployeeId && x.UserId == userResult.Data)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.UserId, (string?)null),
                    CancellationToken.None);
            await userService.DeleteAsync(userResult.Data);
            throw;
        }

        return userResult;
    }
}
