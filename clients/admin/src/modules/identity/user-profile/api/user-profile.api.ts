import { identityApi } from "@/lib/server/backend-api";
import { requestJson } from "@/lib/server/http";
import { guardCall, guardRawCall } from "@/lib/server/call-guard";
import { explicitBearerTokenHandler } from "@/lib/server/http-handlers/bearer-token-handler";
import { ApiClients } from "@/lib/server/api-clients";
import type { Result } from "@/types/api";
import type { UserDto } from "@/modules/identity/users";
import type { UserSessionDto } from "@/modules/identity/user-profile/types/user-session";

/**
 * Takes the access token explicitly rather than reading it from the ambient
 * session — its two call sites (`login-action.ts`, `proxy.ts`) run before the
 * session cookie carrying this token has been written (or, in `proxy.ts`,
 * inside Edge middleware which never has an ambient `next/headers` session).
 */
export function getCurrentUser(accessToken: string) {
  return guardCall(() =>
    requestJson<Result<UserDto>>("user_profile", {
      client: ApiClients.Identity,
      handlers: [explicitBearerTokenHandler(accessToken)],
    }),
  );
}

export function listSessions() {
  return guardRawCall(() =>
    identityApi.requestJson<UserSessionDto[]>("user_profile/token/list"),
  );
}

export function revokeSession(tokenId: string) {
  return guardRawCall(() =>
    identityApi.requestVoid("user_profile/token/revoke", { method: "PUT", body: tokenId }),
  );
}
