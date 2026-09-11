import { identityApi } from "@/lib/server/backend-api";

const { requestJson } = identityApi;
import { guardCall } from "@/lib/server/call-guard";
import type { Result } from "@/types/api";
import type { HubTokenDto } from "@/modules/notifications/types/signalr";

/**
 * Mints a short-lived, notification-hub-scoped access token for the current
 * session. Authenticated: routed through `identityApi` so `bearerTokenHandler`
 * attaches the session bearer — the raw `http` client (used by the pre-auth
 * `token.api.ts` calls) would send this unauthenticated and the backend would
 * reject it.
 */
export function getHubToken() {
  return guardCall(() =>
    requestJson<Result<HubTokenDto>>("auth/token/hub", { method: "POST" }),
  );
}
