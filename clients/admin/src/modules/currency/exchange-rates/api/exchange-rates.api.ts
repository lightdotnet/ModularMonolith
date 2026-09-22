import "server-only";

import { currencyApi } from "@/lib/server/backend-api";
import { guardCall } from "@/lib/server/call-guard";
import type { PagedResult, Result } from "@/types/api";
import type {
  ExchangeRateDto,
  ExchangeRateSearchParams,
  RecordExchangeRateRequest,
} from "../types/exchange-rate";

const { requestJson } = currencyApi;

/** The rate history, newest first. */
export function searchExchangeRates(params: ExchangeRateSearchParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<ExchangeRateDto>>("exchange_rate", {
      method: "GET",
      query: {
        currencyCode: params.currencyCode,
        from: params.from,
        to: params.to,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

/** The rate in effect per active foreign currency (as of now unless `asOf` is given). */
export function getLatestExchangeRates(asOf?: string) {
  return guardCall(() =>
    requestJson<Result<ExchangeRateDto[]>>("exchange_rate/latest", {
      method: "GET",
      query: { asOf },
    }),
  );
}

export function recordExchangeRate(request: RecordExchangeRateRequest) {
  return guardCall(() =>
    requestJson<Result<number | string>>("exchange_rate", { method: "POST", body: request }),
  );
}
