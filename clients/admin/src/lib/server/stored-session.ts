import "server-only";

import { extractAllClaims, extractPermissions, extractRoles } from "@/lib/server/jwt";
import { dedupeClaims } from "@/lib/shared/dedupe-claims";
import type { ClaimDto } from "@/types/claim";
import type { SessionData, StoredSession } from "@/types/session";

/** Claims in `session.claims` that decoding `session.accessToken` doesn't already
 *  produce — i.e. sourced from the profile API. Never includes permission/role,
 *  since those are JWT-only (see `lib/server/jwt.ts`). */
function extraClaimsOf(session: SessionData): ClaimDto[] {
  const jwtKeys = new Set(
    extractAllClaims(session.accessToken).map((claim) => `${claim.type}:${claim.value}`),
  );
  return session.claims.filter((claim) => !jwtKeys.has(`${claim.type}:${claim.value}`));
}

/** Reduces a hydrated session to the minimal shape that's actually persisted. */
export function toStoredSession(session: SessionData): StoredSession {
  return {
    accessToken: session.accessToken,
    expiresAt: session.expiresAt,
    refreshToken: session.refreshToken,
    sessionExpiresAt: session.sessionExpiresAt,
    profile: session.profile,
    refreshFailureCount: session.refreshFailureCount,
    extraClaims: extraClaimsOf(session),
  };
}

/** Rebuilds the full session shape from what's persisted, re-decoding `claims`/`permissions`/`roles` from `accessToken` every time. */
export function hydrateSession(stored: StoredSession): SessionData {
  return {
    accessToken: stored.accessToken,
    expiresAt: stored.expiresAt,
    refreshToken: stored.refreshToken,
    sessionExpiresAt: stored.sessionExpiresAt,
    profile: stored.profile,
    refreshFailureCount: stored.refreshFailureCount,
    claims: dedupeClaims([...extractAllClaims(stored.accessToken), ...(stored.extraClaims ?? [])]),
    permissions: extractPermissions(stored.accessToken),
    roles: extractRoles(stored.accessToken),
  };
}

export function isStoredSession(value: unknown): value is StoredSession {
  if (typeof value !== "object" || value === null) return false;
  const v = value as Record<string, unknown>;

  return (
    typeof v.accessToken === "string" &&
    typeof v.expiresAt === "number" &&
    typeof v.sessionExpiresAt === "number" &&
    (v.refreshToken === null || typeof v.refreshToken === "string")
  );
}
