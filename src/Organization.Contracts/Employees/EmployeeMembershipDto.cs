using StarterKit.Organization.Contracts.OrgUnits;

namespace StarterKit.Organization.Contracts.Employees;

public class EmployeeMembershipDto
{
    public string OrgUnitId { get; set; } = null!;

    public string OrgUnitName { get; set; } = null!;

    public OrgUnitType OrgUnitType { get; set; }

    public string? LevelId { get; set; }

    public string? LevelName { get; set; }

    public bool IsPrimary { get; set; }

    public AssignmentType AssignmentType { get; set; }

    public bool IsManager { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }
}
