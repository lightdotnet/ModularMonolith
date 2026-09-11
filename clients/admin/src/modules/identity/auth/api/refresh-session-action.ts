"use server";

import { revalidatePath } from "next/cache";
import { getSession } from "@/lib/server/session";
import { refreshSession } from "@/lib/server/refresh-session";
import { persistSessionCookie } from "@/lib/server/persist-session-cookie";
import { isSuperAdminUser } from "@/lib/server/authorization";

/**
 * Manually rotates the current session's tokens. Gated on super-admin here —
 * not just at the call site — since a Server Action is a callable endpoint
 * regardless of whether the triggering UI is rendered.
 */
export async function refreshSessionAction(): Promise<void> {
  const session = await getSession();
  if (!session || !isSuperAdminUser(session.profile?.userName)) return;

  const outcome = await refreshSession(session);
  if (outcome.status !== "success") return;

  await persistSessionCookie(outcome.session);

  revalidatePath("/user-profile");
}
