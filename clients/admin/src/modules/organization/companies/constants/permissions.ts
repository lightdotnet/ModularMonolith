/** Mirrors `Organization.Contracts/Authorization/OrganizationPermissions.cs` (Companies.*) — keep the string values in sync with the backend. */
export const COMPANIES_PERMISSIONS = {
  View: "organization.companies.view",
  Create: "organization.companies.create",
  Update: "organization.companies.update",
  Delete: "organization.companies.delete",
} as const;
