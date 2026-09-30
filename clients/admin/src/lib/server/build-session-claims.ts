import "server-only";

import { extractAllClaims } from "@/lib/server/jwt";
import { dedupeClaims } from "@/lib/shared/dedupe-claims";
import type { ClaimDto } from "@/types/claim";

/**
 * Session claims = every claim in the access token (identity, permissions, roles, ...)
 * union'd with the profile API's raw claims — never its `roles` (roles/permissions are
 * JWT-only, see `lib/server/jwt.ts`).
 */
export function buildSessionClaims(accessToken: string, profileClaims: ClaimDto[]): ClaimDto[] {
  return dedupeClaims([...extractAllClaims(accessToken), ...profileClaims]);
}
