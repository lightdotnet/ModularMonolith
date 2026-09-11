using StarterKit.Organization.Api.Data;

namespace StarterKit.Organization.Api.Application.EmployeeLevels.Commands;

internal sealed record UpdateEmployeeLevelCommand(EmployeeLevelDto Model) : ICommand<IResult>;

internal class UpdateEmployeeLevelCommandHandler(OrganizationDbContext context)
    : ICommandHandler<UpdateEmployeeLevelCommand, IResult>
{
    public async Task<IResult> Handle(
        UpdateEmployeeLevelCommand request,
        CancellationToken cancellationToken)
    {
        var model = request.Model;

        var entity = await context.EmployeeLevels
            .FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken);

        if (entity is null)
            return Result.NotFound($"Employee level {model.Id} not found");

        var codeTaken = await context.EmployeeLevels
            .AnyAsync(x => x.Id != model.Id && x.CompanyId == entity.CompanyId && x.Code == model.Code, cancellationToken);

        if (codeTaken)
            return Result.Error($"Employee level code '{model.Code}' already exists in this company.");

        entity.Name = model.Name;
        entity.Code = model.Code;
        entity.Rank = model.Rank;
        entity.Description = model.Description;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
