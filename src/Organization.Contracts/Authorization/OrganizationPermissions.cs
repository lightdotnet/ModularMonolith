namespace StarterKit.Organization.Contracts.Authorization;

public static class OrganizationPermissions
{
    public const string Group = "organization";

    public static class Companies
    {
        public const string View = $"{Group}.companies.view";

        public const string Create = $"{Group}.companies.create";

        public const string Update = $"{Group}.companies.update";

        public const string Delete = $"{Group}.companies.delete";
    }

    public static class OrgUnits
    {
        public const string View = $"{Group}.org_units.view";

        public const string Create = $"{Group}.org_units.create";

        public const string Update = $"{Group}.org_units.update";

        public const string Delete = $"{Group}.org_units.delete";
    }

    public static class EmployeeLevels
    {
        public const string View = $"{Group}.employee_levels.view";

        public const string Create = $"{Group}.employee_levels.create";

        public const string Update = $"{Group}.employee_levels.update";

        public const string Delete = $"{Group}.employee_levels.delete";
    }

    public static class Employees
    {
        public const string View = $"{Group}.employees.view";

        public const string Create = $"{Group}.employees.create";

        public const string Update = $"{Group}.employees.update";

        public const string Delete = $"{Group}.employees.delete";

        public const string ManageLogin = $"{Group}.employees.manage_login";
    }
}
