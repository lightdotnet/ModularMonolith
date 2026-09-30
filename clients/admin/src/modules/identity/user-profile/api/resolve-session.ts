import { getSession } from "@/lib/server/session";
import type { SessionData } from "@/types/session";

/** Reads the session cookie. Profile/claims/roles/permissions are already embedded in it. */
export async function resolveSession(): Promise<SessionData | null> {
  return getSession();
}
