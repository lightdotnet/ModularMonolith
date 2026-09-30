import "server-only";

import type { ClaimDto } from "@/types/claim";

type JwtClaims = Record<string, unknown>;

/**
 * Decodes a JWT payload without verifying its signature. Safe here because the
 * token was just issued by our own backend (login/refresh response) — this is
 * for reading claims client-side-equivalent (server-rendered), not authenticating it.
 */
function decodeJwtClaims(token: string): JwtClaims {
  const payload = token.split(".")[1];
  if (!payload) throw new Error("Invalid JWT token.");
  return JSON.parse(Buffer.from(payload, "base64url").toString("utf8")) as JwtClaims;
}

/** Backend serializes a single-valued claim as a string and multi-valued as a string[]. */
function normalizeClaimValues(value: unknown): string[] {
  if (Array.isArray(value)) return value.map(String);
  if (typeof value === "string") return [value];
  return [];
}

/** Extracts a claim (by JWT claim type) from an access token. Never throws — decode errors yield []. */
function extractClaimValues(accessToken: string, claimType: string): string[] {
  try {
    const claims = decodeJwtClaims(accessToken);
    return normalizeClaimValues(claims[claimType]);
  } catch {
    return [];
  }
}

export function extractPermissions(accessToken: string): string[] {
  return extractClaimValues(accessToken, "permission");
}

export function extractRoles(accessToken: string): string[] {
  return extractClaimValues(accessToken, "role");
}

/** Every claim carried by the access token (uid, un, jti, permission, role, ...), flattened. */
export function extractAllClaims(accessToken: string): ClaimDto[] {
  try {
    const claims = decodeJwtClaims(accessToken);
    return Object.entries(claims).flatMap(([type, value]) =>
      normalizeClaimValues(value).map((claimValue) => ({ type, value: claimValue })),
    );
  } catch {
    return [];
  }
}
