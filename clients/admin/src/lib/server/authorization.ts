import "server-only";

import * as shared from "@/lib/shared/authorization";
import type { SessionData } from "@/types/session";

export const SUPER_ADMIN_USERNAMES = shared.SUPER_ADMIN_USERNAMES;
export const isSuperAdminUser = shared.isSuperAdminUser;

export function hasPermission(
  session: Pick<SessionData, "permissions" | "profile">,
  permission: string,
): boolean {
  return shared.hasPermission(session.permissions, session.profile?.userName, permission);
}

export function hasAnyPermission(
  session: Pick<SessionData, "permissions" | "profile">,
  permissions: string[],
): boolean {
  return shared.hasAnyPermission(session.permissions, session.profile?.userName, permissions);
}

export function hasAllPermissions(
  session: Pick<SessionData, "permissions" | "profile">,
  permissions: string[],
): boolean {
  return shared.hasAllPermissions(session.permissions, session.profile?.userName, permissions);
}
