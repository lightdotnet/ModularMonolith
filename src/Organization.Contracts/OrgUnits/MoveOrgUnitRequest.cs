namespace StarterKit.Organization.Contracts.OrgUnits;

public record MoveOrgUnitRequest
{
    public string? NewParentId { get; set; }
}
