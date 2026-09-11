namespace StarterKit.Organization.Api.Domain.Companies;

public class CompanyByIdSpec : Specification<Company>
{
    public CompanyByIdSpec(string companyId)
    {
        Where(c => c.Id == companyId);
    }
}
