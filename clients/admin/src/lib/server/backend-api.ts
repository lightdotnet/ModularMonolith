import "server-only";

import {
  requestJson as httpRequestJson,
  requestVoid as httpRequestVoid,
  type RequestOptions,
} from "@/lib/server/http";
import { bearerTokenHandler } from "@/lib/server/http-handlers/bearer-token-handler";
import { ApiClients, type ApiClientName } from "@/lib/server/api-clients";

type BackendRequestOptions = Omit<RequestOptions, "client">;

/**
 * Binds `requestJson`/`requestVoid` to one backend module and the ambient
 * session's access token. To target another backend, call this with a
 * different `ApiClientName` and export the result — no need for a sibling
 * file duplicating this module. A service needing a different auth source
 * (e.g. a token that isn't in the session cookie yet) should call
 * `@/lib/server/http` directly instead.
 */
function createBackendApiClient(client: ApiClientName) {
  return {
    requestJson<T>(path: string, options: BackendRequestOptions = {}): Promise<T> {
      return httpRequestJson<T>(path, {
        ...options,
        client,
        handlers: [bearerTokenHandler, ...(options.handlers ?? [])],
      });
    },
    requestVoid(path: string, options: BackendRequestOptions = {}): Promise<void> {
      return httpRequestVoid(path, {
        ...options,
        client,
        handlers: [bearerTokenHandler, ...(options.handlers ?? [])],
      });
    },
  };
}

export const identityApi = createBackendApiClient(ApiClients.Identity);
export const notificationsApi = createBackendApiClient(ApiClients.Notifications);
export const organizationApi = createBackendApiClient(ApiClients.Organization);
export const approvalApi = createBackendApiClient(ApiClients.Approval);
export const leaveManagementApi = createBackendApiClient(ApiClients.LeaveManagement);
