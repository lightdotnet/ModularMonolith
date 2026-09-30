import "server-only";

import { cookies } from "next/headers";
import { encodeSessionCookies } from "@/lib/server/cookie-codec";
import { buildSessionCookieOptions } from "@/lib/server/session-cookie";
import type { SessionData } from "@/types/session";

/**
 * Persists a session to the cookie(s) from a Server Action / Route Handler context.
 * Not usable from middleware — `proxy.ts` sets cookies via `NextRequest`/
 * `NextResponse` instead, so it doesn't import this.
 */
export async function persistSessionCookie(session: SessionData): Promise<void> {
  const cookieStore = await cookies();
  const encoded = encodeSessionCookies(session);
  const options = buildSessionCookieOptions(session.sessionExpiresAt);

  for (const { name, value } of encoded.cookies) cookieStore.set(name, value, options);
  for (const name of encoded.stale) {
    if (cookieStore.get(name)) cookieStore.delete(name);
  }
}
