using StarterKit.Shared.Entities;

namespace StarterKit.Organization.Api.Domain.OrgUnits;

public class OrgUnit : AuditableEntity
{
    public string CompanyId { get; set; } = null!;

    public string? ParentId { get; set; }

    public virtual OrgUnit? Parent { get; set; }

    public virtual IList<OrgUnit> Children { get; set; } = [];

    public OrgUnitType Type { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? ManagerEmployeeId { get; set; }

    public string? Description { get; set; }

    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    public virtual IList<EmployeeOrgUnitMembership> Memberships { get; set; } = [];
}
