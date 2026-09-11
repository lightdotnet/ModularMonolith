using StarterKit.Organization.Contracts.Common;

namespace StarterKit.Organization.Contracts.OrgUnits;

public class OrgUnitTreeNodeDto : BaseDto
{
    public string? ParentId { get; set; }

    public OrgUnitType Type { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? ManagerEmployeeId { get; set; }

    public OrganizationStatus Status { get; set; }

    public IList<OrgUnitTreeNodeDto> Children { get; set; } = [];
}
