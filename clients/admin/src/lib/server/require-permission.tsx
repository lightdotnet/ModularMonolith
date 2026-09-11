import "server-only";

import { redirect } from "next/navigation";
import { AccessDenied } from "@/components/shared/access-denied";
import { resolveSession } from "@/modules/identity/user-profile";
import { hasPermission } from "@/lib/server/authorization";
import type { SessionData } from "@/types/session";

type PermissionCheck =
  | { session: SessionData; denied: null }
  | { session: SessionData; denied: React.ReactElement };

/**
 * Resolves the session (redirecting to `/login` if there is none) and checks
 * `permission`, returning the `<AccessDenied>` element to render if it fails.
 * Kept as a function rather than a wrapping component so pages can still
 * `if (denied) return denied;` before doing their data fetch, same as the
 * inline check this replaces.
 */
export async function requirePermission(permission: string): Promise<PermissionCheck> {
  const session = await resolveSession();
  if (!session) {
    redirect("/login");
  }

  if (!hasPermission(session, permission)) {
    return { session, denied: <AccessDenied permission={permission} /> };
  }

  return { session, denied: null };
}
