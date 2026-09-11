using Mapster;
using StarterKit.Organization.Api.Data;
using StarterKit.Organization.Api.Domain.Employees;

namespace StarterKit.Organization.Api.Application.Employees.Queries;

internal sealed record GetEmployeeByIdQuery(string Id) : IQuery<IResult<EmployeeDto>>;

internal class GetEmployeeByIdQueryHandler(OrganizationDbContext context)
    : IQueryHandler<GetEmployeeByIdQuery, IResult<EmployeeDto>>
{
    public async Task<IResult<EmployeeDto>> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await context.Employees
            .AsNoTracking()
            .Where(new EmployeeByIdSpec(request.Id))
            .ProjectToType<EmployeeDto>()
            .SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Result<EmployeeDto>.NotFound($"Employee {request.Id} not found");

        dto.Memberships = await context.EmployeeOrgUnitMemberships
            .AsNoTracking()
            .Where(x => x.EmployeeId == request.Id && x.EndDate == null)
            .Select(x => new EmployeeMembershipDto
            {
                OrgUnitId = x.OrgUnitId,
                OrgUnitName = x.OrgUnit.Name,
                OrgUnitType = x.OrgUnit.Type,
                LevelId = x.LevelId,
                LevelName = x.Level != null ? x.Level.Name : null,
                IsPrimary = x.IsPrimary,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
            })
            .ToListAsync(cancellationToken);

        return Result<EmployeeDto>.Success(dto);
    }
}
