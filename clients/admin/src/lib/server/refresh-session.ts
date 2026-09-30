import "server-only";

import { refreshToken } from "@/modules/identity/auth/api/token.api";
import { extractPermissions, extractRoles } from "@/lib/server/jwt";
import { REFRESH_LEAD_MS } from "@/lib/server/session-cookie";
import type { ResultCode } from "@/types/api";
import type { SessionData } from "@/types/session";

/** Statuses that mean the refresh token itself is dead — not going to succeed on retry, unlike a network blip or backend outage. */
const PERMANENT_FAILURE_CODES: ReadonlySet<ResultCode> = new Set(["unauthorized", "bad_request"]);

export type RefreshOutcome =
  | { status: "skipped" }
  | { status: "success"; session: SessionData }
  | { status: "failed"; permanent: boolean };

function logRefresh(message: string): void {
  if (process.env.NODE_ENV !== "development") return;
  console.log(`[refresh-session] ${message}`);
}

/**
 * Attempts to rotate the access/refresh token. A `"failed"` outcome is not
 * proof the session is dead by itself — a multi-tab rotation race produces
 * the same shape as a truly revoked token; only `permanent: true` (401/400)
 * is meaningful, and callers should still tolerate a few of those before
 * treating the session as dead, since the race self-heals on the very next
 * request. `permanent: false` (network/5xx) says nothing about token
 * validity and should never count toward that decision.
 */
export async function refreshSession(session: SessionData): Promise<RefreshOutcome> {
  if (!session.refreshToken) {
    logRefresh("no refresh token on session — treating as permanent failure");
    return { status: "failed", permanent: true };
  }

  logRefresh("attempting refresh");

  const result = await refreshToken({
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
  });

  if (!result.isSuccess || !result.data) {
    const permanent = PERMANENT_FAILURE_CODES.has(result.code);
    logRefresh(`refresh failed (${permanent ? "permanent" : "transient"}, code=${result.code}): ${result.message}`);
    return { status: "failed", permanent };
  }

  const token = result.data;
  logRefresh(`refresh succeeded, new expiry in ${token.expiresIn}s`);
  return {
    status: "success",
    session: {
      ...session,
      accessToken: token.accessToken,
      expiresAt: Date.now() + token.expiresIn * 1000,
      refreshToken: token.refreshToken,
      // Roles/permissions live in the JWT — re-decode from the freshly-issued token.
      permissions: extractPermissions(token.accessToken),
      roles: extractRoles(token.accessToken),
      refreshFailureCount: 0,
    },
  };
}

/** Refreshes only if the access token is within `REFRESH_LEAD_MS` of expiring. */
export async function refreshSessionIfNearExpiry(session: SessionData): Promise<RefreshOutcome> {
  if (session.expiresAt - Date.now() > REFRESH_LEAD_MS) return { status: "skipped" };
  return refreshSession(session);
}
