namespace StarterKit.Organization.Api.Domain.OrgUnits;

public class ActiveEmployeeOrgUnitMembershipSpec : Specification<EmployeeOrgUnitMembership>
{
    public ActiveEmployeeOrgUnitMembershipSpec(string employeeId, string orgUnitId)
    {
        Where(x => x.EmployeeId == employeeId && x.OrgUnitId == orgUnitId && x.EndDate == null);
    }
}
