import "server-only";

import { decrypt, encrypt } from "@/lib/server/token-cipher";
import {
  ALL_SESSION_COOKIE_NAMES,
  chunkName,
  MAX_CHUNK_BYTES,
  MAX_CHUNKS,
  SESSION_COOKIE_NAME,
} from "@/lib/server/session-cookie";
import { hydrateSession, isStoredSession, toStoredSession } from "@/lib/server/stored-session";
import type { SessionData } from "@/types/session";

export interface EncodedSessionCookies {
  /** `{ name, value }` pairs to `set`, in order. One entry for a small session, N for a chunked one. */
  cookies: { name: string; value: string }[];
  /** Names that must be cleared because this write doesn't use them (stale chunks, or the base name when chunking). */
  stale: string[];
}

/** Encrypts and (if needed) splits a session into the cookies that carry it.
 *  Throws — a real, loud failure — if it still can't fit within MAX_CHUNKS,
 *  instead of silently dropping data like the original bug did. */
export function encodeSessionCookies(session: SessionData): EncodedSessionCookies {
  const payload = encrypt(JSON.stringify(toStoredSession(session)));

  if (payload.length <= MAX_CHUNK_BYTES) {
    return {
      cookies: [{ name: SESSION_COOKIE_NAME, value: payload }],
      stale: ALL_SESSION_COOKIE_NAMES.filter((name) => name !== SESSION_COOKIE_NAME),
    };
  }

  const parts: string[] = [];
  for (let i = 0; i < payload.length; i += MAX_CHUNK_BYTES) {
    parts.push(payload.slice(i, i + MAX_CHUNK_BYTES));
  }
  if (parts.length > MAX_CHUNKS) {
    throw new Error(
      `Session cookie too large: ${payload.length} bytes across ${parts.length} chunks (max ${MAX_CHUNKS}).`,
    );
  }

  const used = parts.map((_, i) => chunkName(i));
  return {
    cookies: parts.map((value, i) => ({ name: chunkName(i), value })),
    stale: ALL_SESSION_COOKIE_NAMES.filter((name) => !used.includes(name)),
  };
}

/** Reassembles + decrypts + hydrates. Prefers the single base cookie; otherwise
 *  concatenates `<name>.0..N` until the first gap. Any tampering, a missing
 *  chunk, or a shape mismatch fails closed (returns null) rather than
 *  crashing or trusting partial data. */
export function decodeSessionCookies(
  read: (name: string) => string | undefined,
): SessionData | null {
  const single = read(SESSION_COOKIE_NAME);
  let raw = single ?? "";

  if (!single) {
    for (let i = 0; i < MAX_CHUNKS; i++) {
      const part = read(chunkName(i));
      if (part === undefined) break;
      raw += part;
    }
  }
  if (!raw) return null;

  const plaintext = decrypt(raw);
  if (!plaintext) return null;

  try {
    const parsed: unknown = JSON.parse(plaintext);
    return isStoredSession(parsed) ? hydrateSession(parsed) : null;
  } catch {
    return null;
  }
}
