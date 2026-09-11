/** Mirrors `Identity.Contracts/Authorization/IdentityPermissions.cs` (Users.*) — keep the string values in sync with the backend. */
export const USERS_PERMISSIONS = {
  View: "identity.users.view",
  Create: "identity.users.create",
  Update: "identity.users.update",
  Delete: "identity.users.delete",
} as const;
