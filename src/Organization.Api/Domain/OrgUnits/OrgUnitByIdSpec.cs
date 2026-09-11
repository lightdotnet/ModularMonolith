namespace StarterKit.Organization.Api.Domain.OrgUnits;

public class OrgUnitByIdSpec : Specification<OrgUnit>
{
    public OrgUnitByIdSpec(string orgUnitId)
    {
        Where(x => x.Id == orgUnitId);
    }
}
