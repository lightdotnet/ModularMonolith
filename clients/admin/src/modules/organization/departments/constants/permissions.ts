/** Mirrors `Organization.Contracts/Authorization/OrganizationPermissions.cs` (OrgUnits.*) — keep the string values in sync with the backend. */
export const ORG_UNITS_PERMISSIONS = {
  View: "organization.org_units.view",
  Create: "organization.org_units.create",
  Update: "organization.org_units.update",
  Delete: "organization.org_units.delete",
} as const;

/** Mirrors `Organization.Contracts/Authorization/OrganizationPermissions.cs` (EmployeeLevels.*). */
export const EMPLOYEE_LEVELS_PERMISSIONS = {
  View: "organization.employee_levels.view",
  Create: "organization.employee_levels.create",
  Update: "organization.employee_levels.update",
  Delete: "organization.employee_levels.delete",
} as const;
