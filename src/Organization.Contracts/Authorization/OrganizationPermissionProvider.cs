using Light.AspNetCore.Authorization;

namespace StarterKit.Organization.Contracts.Authorization;

public class OrganizationPermissionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<PermissionDefinition> Define()
    {
        yield return new(
            OrganizationPermissions.Companies.View,
            "View Companies",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Companies.Create,
            "Create Companies",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Companies.Update,
            "Update Companies",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Companies.Delete,
            "Delete Companies",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.OrgUnits.View,
            "View Departments & Teams",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.OrgUnits.Create,
            "Create Departments & Teams",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.OrgUnits.Update,
            "Update Departments & Teams",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.OrgUnits.Delete,
            "Delete Departments & Teams",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.EmployeeLevels.View,
            "View Employee Levels",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.EmployeeLevels.Create,
            "Create Employee Levels",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.EmployeeLevels.Update,
            "Update Employee Levels",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.EmployeeLevels.Delete,
            "Delete Employee Levels",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Employees.View,
            "View Employees",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Employees.Create,
            "Create Employees",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Employees.Update,
            "Update Employees",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Employees.Delete,
            "Delete Employees",
            OrganizationPermissions.Group);

        yield return new(
            OrganizationPermissions.Employees.ManageLogin,
            "Manage Employee Login Accounts",
            OrganizationPermissions.Group);
    }
}
