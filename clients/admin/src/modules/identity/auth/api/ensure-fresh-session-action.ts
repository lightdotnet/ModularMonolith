"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getSession } from "@/lib/server/session";
import { refreshSessionIfNearExpiry } from "@/lib/server/refresh-session";
import { refetchProfile } from "@/lib/server/refetch-profile";
import { persistSessionCookie } from "@/lib/server/persist-session-cookie";
import {
  ALL_SESSION_COOKIE_NAMES,
  MAX_REFRESH_FAILURES,
} from "@/lib/server/session-cookie";

export type EnsureFreshSessionResult =
  | { status: "fresh" }
  | { status: "updated" }
  | { status: "retry" }
  | { status: "degraded" };

/**
 * Called client-side (by `SessionGate`) instead of from blocking middleware,
 * so a loading skeleton can be shown while this runs. Mirrors the logic that
 * used to live in `proxy.ts`: rotates the token when near expiry, tracks
 * consecutive *permanent* (401/400) failures and force-logs-out at
 * `MAX_REFRESH_FAILURES`, and never punishes a transient (network/5xx)
 * failure — see `refreshSession`'s doc comment in `refresh-session.ts`.
 */
export async function ensureFreshSessionAction(options: {
  refetchProfile: boolean;
}): Promise<EnsureFreshSessionResult> {
  const session = await getSession();
  if (!session) return { status: "fresh" };

  const outcome = await refreshSessionIfNearExpiry(session);

  if (outcome.status === "failed" && outcome.permanent) {
    const failureCount = (session.refreshFailureCount ?? 0) + 1;
    if (failureCount >= MAX_REFRESH_FAILURES) {
      const cookieStore = await cookies();
      for (const name of ALL_SESSION_COOKIE_NAMES) cookieStore.delete(name);
      redirect("/login");
    }
    await persistSessionCookie({ ...session, refreshFailureCount: failureCount });
    return { status: "degraded" };
  }

  if (outcome.status === "failed") return { status: "retry" };

  const tokenWasRefreshed = outcome.status === "success";
  if (!options.refetchProfile && !tokenWasRefreshed) return { status: "fresh" };

  const nextSession = tokenWasRefreshed ? outcome.session : session;
  await persistSessionCookie(await refetchProfile(nextSession));
  return { status: "updated" };
}
