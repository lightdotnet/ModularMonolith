using StarterKit.Shared.Entities;

namespace StarterKit.Organization.Api.Domain.Companies;

public class Company : AuditableEntity
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? TaxCode { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? Description { get; set; }

    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;
}
