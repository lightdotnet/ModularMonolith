using Light.EntityFrameworkCore.Extensions;
using StarterKit.Organization.Api.Data;
using StarterKit.Persistence.Extensions;

namespace StarterKit.Organization.Api.Application.Employees.Queries;

internal sealed record SearchEmployeesQuery(
    EmployeeSearchRequest Request) : IQuery<PagedResult<EmployeeDto>>;

internal class SearchEmployeesQueryHandler(OrganizationDbContext context)
    : IQueryHandler<SearchEmployeesQuery, PagedResult<EmployeeDto>>
{
    public Task<PagedResult<EmployeeDto>> Handle(
        SearchEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var lookup = request.Request;

        var query = context.Employees
            .AsNoTracking()
            .WhereIf(!string.IsNullOrEmpty(lookup.CompanyId), x => x.CompanyId == lookup.CompanyId)
            .WhereIf(lookup.EmploymentStatus.HasValue, x => x.EmploymentStatus == lookup.EmploymentStatus!.Value)
            .WhereIf(lookup.LinkedToUserOnly == true, x => x.UserId != null)
            .WhereIf(!string.IsNullOrEmpty(lookup.SearchValue),
                x => x.FirstName.Contains(lookup.SearchValue!)
                    || x.LastName.Contains(lookup.SearchValue!)
                    || (x.FirstName + " " + x.LastName).Contains(lookup.SearchValue!)
                    || x.EmployeeCode.Contains(lookup.SearchValue!)
                    || x.Email!.Contains(lookup.SearchValue!));

        if (!string.IsNullOrEmpty(lookup.OrgUnitId))
        {
            query = query.Where(x => x.Memberships.Any(m => m.OrgUnitId == lookup.OrgUnitId && m.EndDate == null));
        }

        // Employee search is company-wide readable (any authenticated user) so it exposes only
        // basic directory fields. Sensitive PII (national id, date of birth, address, ...) is
        // omitted here and served solely by GET employee/{id}, which stays permission-gated.
        return query
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new EmployeeDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                UserId = x.UserId,
                EmployeeCode = x.EmployeeCode,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                EmploymentStatus = x.EmploymentStatus,
                AvatarUrl = x.AvatarUrl,
            })
            .ToPagedResultAsync(lookup, cancellationToken);
    }
}
