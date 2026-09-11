import "server-only";

import { getCurrentUser } from "@/modules/identity/user-profile/api/user-profile.api";
import { buildSessionClaims } from "@/lib/server/build-session-claims";
import type { ProfileData, SessionData } from "@/types/session";

/**
 * Refetches profile display data (name/email/status) and rebuilds `claims` as the
 * union of the access token's own claims and the profile API's raw claims — never
 * its `roles` (roles/permissions only ever come from the JWT). Returns `session`
 * unchanged on failure.
 */
export async function refetchProfile(session: SessionData): Promise<SessionData> {
  const result = await getCurrentUser(session.accessToken);
  if (!result.isSuccess || !result.data) return session;

  const profileDto = result.data;
  const profile: ProfileData = {
    id: profileDto.id,
    userName: profileDto.userName,
    firstName: profileDto.firstName,
    lastName: profileDto.lastName,
    email: profileDto.email,
    phoneNumber: profileDto.phoneNumber,
    status: profileDto.status,
    authProvider: profileDto.authProvider,
    isDeleted: profileDto.isDeleted,
  };

  return {
    ...session,
    claims: buildSessionClaims(session.accessToken, profileDto.claims),
    profile,
  };
}
