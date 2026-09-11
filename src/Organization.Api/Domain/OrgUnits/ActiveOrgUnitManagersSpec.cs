namespace StarterKit.Organization.Api.Domain.OrgUnits;

public class ActiveOrgUnitManagersSpec : Specification<EmployeeOrgUnitMembership>
{
    public ActiveOrgUnitManagersSpec(string orgUnitId)
    {
        Where(x => x.OrgUnitId == orgUnitId && x.EndDate == null && x.IsManager);
    }
}
