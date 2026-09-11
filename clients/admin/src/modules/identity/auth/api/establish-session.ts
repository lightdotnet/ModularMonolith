import "server-only";

import { getCurrentUser } from "@/modules/identity/user-profile";
import { extractPermissions, extractRoles } from "@/lib/server/jwt";
import { buildSessionClaims } from "@/lib/server/build-session-claims";
import { persistSessionCookie } from "@/lib/server/persist-session-cookie";
import { SESSION_TTL_MS } from "@/lib/server/session-cookie";
import type { TokenDto } from "@/modules/identity/auth/types/token";
import type { ProfileData, SessionData } from "@/types/session";

/**
 * Establishes a session from a freshly issued token — profile fetch,
 * claims/roles/permissions extraction, and cookie persistence. Shared by
 * every path that ends with a `TokenDto` (password login, Microsoft external
 * login) so the resulting session is identical regardless of how the token
 * was obtained. Callable from a Server Action or Route Handler context only
 * (writes cookies via `persistSessionCookie`).
 */
export async function establishSession(token: TokenDto): Promise<void> {
  // Profile/claims fetch failure shouldn't block a successful login — just
  // start the session with no claims/profile rather than failing the whole sign-in.
  const profileResult = await getCurrentUser(token.accessToken);
  const profileDto =
    profileResult.isSuccess && profileResult.data ? profileResult.data : null;

  // JWT claims are always available regardless of whether the profile fetch succeeded.
  const claims = buildSessionClaims(token.accessToken, profileDto?.claims ?? []);
  const profile: ProfileData | null = profileDto
    ? {
        id: profileDto.id,
        userName: profileDto.userName,
        firstName: profileDto.firstName,
        lastName: profileDto.lastName,
        email: profileDto.email,
        phoneNumber: profileDto.phoneNumber,
        status: profileDto.status,
        authProvider: profileDto.authProvider,
        isDeleted: profileDto.isDeleted,
      }
    : null;

  const now = Date.now();
  const sessionExpiresAt = now + SESSION_TTL_MS;

  const session: SessionData = {
    accessToken: token.accessToken,
    expiresAt: now + token.expiresIn * 1000,
    refreshToken: token.refreshToken,
    sessionExpiresAt,
    claims,
    // Roles/permissions come from the JWT only — never from the profile API.
    permissions: extractPermissions(token.accessToken),
    roles: extractRoles(token.accessToken),
    profile,
  };

  await persistSessionCookie(session);
}
