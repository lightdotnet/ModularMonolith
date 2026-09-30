// Update this list for the new project — usernames here bypass every permission check.
export const SUPER_ADMIN_USERNAMES: string[] = ["super"];

export function isSuperAdminUser(userName: string | null | undefined): boolean {
  return !!userName && SUPER_ADMIN_USERNAMES.includes(userName);
}

export function hasPermission(
  permissions: string[],
  userName: string | null | undefined,
  permission: string,
): boolean {
  return isSuperAdminUser(userName) || permissions.includes(permission);
}

export function hasAnyPermission(
  permissions: string[],
  userName: string | null | undefined,
  requiredPermissions: string[],
): boolean {
  return (
    isSuperAdminUser(userName) || requiredPermissions.some((p) => permissions.includes(p))
  );
}

export function hasAllPermissions(
  permissions: string[],
  userName: string | null | undefined,
  requiredPermissions: string[],
): boolean {
  return (
    isSuperAdminUser(userName) || requiredPermissions.every((p) => permissions.includes(p))
  );
}
