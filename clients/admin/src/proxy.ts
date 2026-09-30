import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { decodeSessionCookies } from "@/lib/server/cookie-codec";
import { ALL_SESSION_COOKIE_NAMES } from "@/lib/server/session-cookie";

const LOGIN_PATH = "/login";

/**
 * Exact paths reachable without an existing session — a small explicit list
 * (not a prefix match) so a future unrelated route nested under `/login/*`
 * doesn't accidentally become public. The two Microsoft ones are Route
 * Handlers for the external-login relay: `start` kicks it off before any
 * session exists, `callback` is where the session actually gets established.
 */
const PUBLIC_AUTH_PATHS: readonly string[] = [
  LOGIN_PATH,
  "/login/microsoft/start",
  "/login/microsoft/callback",
];

function loginRedirect(request: NextRequest): NextResponse {
  const loginUrl = new URL(LOGIN_PATH, request.url);
  loginUrl.searchParams.set(
    "redirect",
    `${request.nextUrl.pathname}${request.nextUrl.search}`,
  );
  return NextResponse.redirect(loginUrl);
}

/**
 * Thin auth gate only — enforces the 7-day session cap and keeps `/login`
 * unreachable once authenticated. Token refresh and profile freshness are no
 * longer handled here: they're driven client-side by `SessionGate` (a Server
 * Action + loading skeleton), since middleware blocks the whole navigation
 * with no way to show UI while it runs. See `ensure-fresh-session-action.ts`.
 */
export async function proxy(request: NextRequest) {
  const isPublicAuthPath = PUBLIC_AUTH_PATHS.includes(request.nextUrl.pathname);
  const session = decodeSessionCookies((name) => request.cookies.get(name)?.value);

  const now = Date.now();
  const sessionExpired = !session || session.sessionExpiresAt <= now;

  if (sessionExpired) {
    if (isPublicAuthPath) return NextResponse.next();

    const response = loginRedirect(request);
    for (const name of ALL_SESSION_COOKIE_NAMES) response.cookies.delete(name);
    return response;
  }

  if (isPublicAuthPath) {
    return NextResponse.redirect(new URL("/", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|api).*)"],
};
