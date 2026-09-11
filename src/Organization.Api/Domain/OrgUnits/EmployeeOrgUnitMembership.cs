using StarterKit.Organization.Api.Domain.Employees;
using StarterKit.Organization.Contracts.Employees;
using StarterKit.Shared.Entities;

namespace StarterKit.Organization.Api.Domain.OrgUnits;

public class EmployeeOrgUnitMembership : AuditableEntity
{
    public string EmployeeId { get; set; } = null!;

    public Employee Employee { get; set; } = null!;

    public string OrgUnitId { get; set; } = null!;

    public OrgUnit OrgUnit { get; set; } = null!;

    public string? LevelId { get; set; }

    public EmployeeLevel? Level { get; set; }

    public bool IsPrimary { get; set; }

    public AssignmentType AssignmentType { get; set; } = AssignmentType.Current;

    public bool IsManager { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }
}
