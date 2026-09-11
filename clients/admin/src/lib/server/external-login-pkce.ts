import "server-only";

/**
 * Short-lived cookie carrying the PKCE `code_verifier` between
 * `/login/microsoft/start` (sets it) and `/login/microsoft/callback` (reads +
 * clears it) — never sent to the backend itself, only used locally by this
 * app to complete the authorization-code exchange. Scoped to this app's own
 * `/login/microsoft` path so it's never sent on any other request.
 */
export const EXTERNAL_LOGIN_PKCE_COOKIE_NAME = "microsoft_login_pkce";
export const EXTERNAL_LOGIN_PKCE_COOKIE_PATH = "/login/microsoft";

const PKCE_COOKIE_TTL_SECONDS = 5 * 60;

/** Shared `cookies().set()` options (Route Handler context only). */
export function buildPkceCookieOptions() {
  return {
    httpOnly: true as const,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: EXTERNAL_LOGIN_PKCE_COOKIE_PATH,
    maxAge: PKCE_COOKIE_TTL_SECONDS,
  };
}
