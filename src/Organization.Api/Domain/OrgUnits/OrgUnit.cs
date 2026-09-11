using StarterKit.Shared.Entities;

namespace StarterKit.Organization.Api.Domain.OrgUnits;

public class OrgUnit : AuditableEntity
{
    public string CompanyId { get; set; } = null!;

    public string? ParentId { get; set; }

    public OrgUnit? Parent { get; set; }

    public IList<OrgUnit> Children { get; set; } = [];

    public OrgUnitType Type { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? ManagerEmployeeId { get; set; }

    public string? Description { get; set; }

    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    public IList<EmployeeOrgUnitMembership> Memberships { get; set; } = [];
}
