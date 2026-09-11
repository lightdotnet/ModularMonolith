/** Mirrors `Organization.Contracts/Authorization/OrganizationPermissions.cs` (Employees.*) — keep the string values in sync with the backend. */
export const EMPLOYEES_PERMISSIONS = {
  View: "organization.employees.view",
  Create: "organization.employees.create",
  Update: "organization.employees.update",
  Delete: "organization.employees.delete",
  ManageLogin: "organization.employees.manage_login",
} as const;
