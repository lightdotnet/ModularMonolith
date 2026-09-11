namespace StarterKit.Organization.Contracts.OrgUnits;

public record CreateOrgUnitRequest
{
    public string CompanyId { get; set; } = null!;

    public string? ParentId { get; set; }

    public OrgUnitType Type { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? ManagerEmployeeId { get; set; }

    public string? Description { get; set; }
}
