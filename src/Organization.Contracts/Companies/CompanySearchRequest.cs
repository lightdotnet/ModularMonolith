using StarterKit.Organization.Contracts.Common;

namespace StarterKit.Organization.Contracts.Companies;

public record CompanySearchRequest : SearchQuery
{
    public OrganizationStatus? Status { get; set; }
}
