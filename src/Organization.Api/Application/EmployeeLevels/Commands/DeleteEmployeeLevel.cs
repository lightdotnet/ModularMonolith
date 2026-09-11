using StarterKit.Organization.Api.Data;

namespace StarterKit.Organization.Api.Application.EmployeeLevels.Commands;

internal sealed record DeleteEmployeeLevelCommand(string Id) : ICommand<IResult>;

internal class DeleteEmployeeLevelCommandHandler(OrganizationDbContext context)
    : ICommandHandler<DeleteEmployeeLevelCommand, IResult>
{
    public async Task<IResult> Handle(
        DeleteEmployeeLevelCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await context.EmployeeLevels
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity is null)
            return Result.NotFound($"Employee level {request.Id} not found");

        context.EmployeeLevels.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
