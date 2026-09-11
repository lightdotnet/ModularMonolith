import "server-only";

import { cookies } from "next/headers";
import { decodeSessionCookies } from "@/lib/server/cookie-codec";
import type { SessionData } from "@/types/session";

export async function getSession(): Promise<SessionData | null> {
  const cookieStore = await cookies();
  return decodeSessionCookies((name) => cookieStore.get(name)?.value);
}
