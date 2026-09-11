"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { refreshSessionIfNearExpiry } from "@/lib/server/refresh-session";
import { persistSessionCookie } from "@/lib/server/persist-session-cookie";
import { getSignalRHubUrl } from "@/lib/server/config";
import { getHubToken } from "@/modules/notifications/api/signalr.api";

export interface SignalRTokenState {
  accessToken: string;
  hubUrl: string;
}

/**
 * Hands the browser what it needs to open the notification hub connection
 * directly: a short-lived, hub-audience-only access token and the hub URL.
 * The token is minted per call by the Identity backend (`POST auth/token/hub`)
 * and lives ~2 minutes — the full session JWT never leaves the httpOnly
 * session cookie. The URL is resolved server-side here instead of
 * `NEXT_PUBLIC_`-inlined so it stays a runtime setting — see `getSignalRHubUrl`.
 *
 * Minting the hub token is itself an authenticated call, so the session bearer
 * has to be valid: refresh it proactively first if it's near expiry, mirroring
 * `proxy.ts` — that middleware skips `/api` paths, so a long-lived session on
 * one page (no navigation) would otherwise present an expired bearer and get a
 * 401. The client re-invokes this action on every (re)connect, so each
 * connection attempt gets a freshly minted token.
 */
export async function getSignalRTokenAction(): Promise<SignalRTokenState | null> {
  const session = await resolveSession();
  if (!session) return null;

  const outcome = await refreshSessionIfNearExpiry(session);
  const activeSession = outcome.status === "success" ? outcome.session : session;
  if (outcome.status === "success") await persistSessionCookie(activeSession);

  const result = await getHubToken();
  if (!result.isSuccess || !result.data) return null;

  return { accessToken: result.data.accessToken, hubUrl: getSignalRHubUrl() };
}
