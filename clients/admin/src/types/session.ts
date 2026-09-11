import type { ClaimDto } from "@/types/claim";
import type { UserDto } from "@/modules/identity/users";

/** Display-only profile data. Excludes `roles`/`claims` — those have their own session fields. */
export type ProfileData = Omit<UserDto, "roles" | "claims">;

export interface SessionData {
  accessToken: string;
  /** Absolute expiry of `accessToken`, in ms since epoch. */
  expiresAt: number;
  refreshToken: string | null;
  /** Absolute session expiry (login time + 7 days), in ms since epoch. Not extended by refresh. */
  sessionExpiresAt: number;
  claims: ClaimDto[];
  /** Decoded from the access token's "permission" claim — never from the profile API. */
  permissions: string[];
  /** Decoded from the access token's "role" claim — never from the profile API. */
  roles: string[];
  profile: ProfileData | null;
  /** Consecutive permanent (401/400) refresh failures. Optional so cookies issued before this field existed are treated as 0. Reset to 0 on any successful refresh. */
  refreshFailureCount?: number;
}

/**
 * The session as actually persisted in the encrypted cookie — deliberately
 * minimal. `claims`/`permissions`/`roles` are NOT stored: they're derivable
 * from decoding `accessToken` (see `lib/server/stored-session.ts`), so
 * persisting them too would triplicate the same permission data and risk
 * pushing a broad-permission account's cookie past the browser's silent
 * ~4096-byte limit. `extraClaims` carries only the delta of `claims` that
 * decoding `accessToken` alone wouldn't reproduce (i.e. sourced from the
 * profile API).
 */
export interface StoredSession {
  accessToken: string;
  expiresAt: number;
  refreshToken: string | null;
  sessionExpiresAt: number;
  profile: ProfileData | null;
  refreshFailureCount?: number;
  /** Optional so cookies written before this field existed still parse (hydrates as no extra claims). */
  extraClaims?: ClaimDto[];
}
