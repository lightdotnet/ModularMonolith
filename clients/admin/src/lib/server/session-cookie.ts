import "server-only";

export const SESSION_COOKIE_NAME = "admin_session";

/**
 * Max chars (== bytes; payload is ASCII base64) for a single cookie's value
 * before splitting into numbered chunk cookies. The browser rejects a cookie
 * whose whole `name=value; attrs` string exceeds ~4096 bytes, silently —
 * this leaves generous room for the name/attributes/per-cookie overhead.
 */
export const MAX_CHUNK_BYTES = 3072;

/** Hard ceiling on chunk count — a guard against a pathologically large session. */
export const MAX_CHUNKS = 8;

export function chunkName(index: number): string {
  return `${SESSION_COOKIE_NAME}.${index}`;
}

/** Every cookie name the session could ever occupy — the base name plus every possible chunk slot. */
export const ALL_SESSION_COOKIE_NAMES: readonly string[] = [
  SESSION_COOKIE_NAME,
  ...Array.from({ length: MAX_CHUNKS }, (_, i) => chunkName(i)),
];

/** Hard cap on session lifetime, counted from login — not extended by refresh. */
export const SESSION_TTL_MS = 7 * 24 * 60 * 60 * 1000;

/** How long before access-token expiry the middleware proactively refreshes it. */
export const REFRESH_LEAD_MS = 5 * 60 * 1000;

/** Consecutive permanent (401/400) refresh failures before a session is treated as dead and force-logged-out. */
export const MAX_REFRESH_FAILURES = 3;

/** Shared `cookies().set()` options for the session cookie (Server Action / Route Handler context). */
export function buildSessionCookieOptions(sessionExpiresAt: number) {
  return {
    httpOnly: true as const,
    sameSite: "lax" as const,
    path: "/",
    // Persistent cookie tied to the 7-day session cap, not the short-lived access token.
    maxAge: Math.floor((sessionExpiresAt - Date.now()) / 1000),
  };
}
