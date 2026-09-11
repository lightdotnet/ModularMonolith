import { requestJson } from "@/lib/server/http";
import { guardCall } from "@/lib/server/call-guard";
import { ApiClients } from "@/lib/server/api-clients";
import type { Result } from "@/types/api";
import type {
  DeviceDto,
  ExchangeExternalLoginCodeRequest,
  GetTokenRequest,
  RefreshTokenRequest,
  TokenDto,
} from "@/modules/identity/auth/types/token";

export function getToken(request: GetTokenRequest, device?: Pick<DeviceDto, "id" | "name">) {
  // TokenController's own route ("token") plus its action route ("token/get")
  // combine into a doubled "token/token" path segment on the backend.
  return guardCall(() =>
    requestJson<Result<TokenDto>>("auth/token/get", {
      client: ApiClients.Identity,
      method: "POST",
      body: request,
      query: {
        deviceId: device?.id ?? undefined,
        deviceName: device?.name ?? undefined,
      },
    }),
  );
}

/**
 * Redeems the one-time authorization code from the Microsoft external-login
 * relay (`Identity.Web`'s `ExternalLoginStartModel`/`ExternalLoginRelayModel`)
 * for a token, proving possession of the `code_verifier` matching the
 * `codeChallenge` sent when the relay was started.
 */
export function exchangeExternalLoginCode(code: string, codeVerifier: string) {
  const request: ExchangeExternalLoginCodeRequest = { code, codeVerifier };
  return guardCall(() =>
    requestJson<Result<TokenDto>>("auth/token/external", {
      client: ApiClients.Identity,
      method: "POST",
      body: request,
    }),
  );
}

export function refreshToken(request: RefreshTokenRequest) {
  return guardCall(() =>
    requestJson<Result<TokenDto>>("auth/token/refresh", {
      client: ApiClients.Identity,
      method: "POST",
      body: request,
    }),
  );
}
