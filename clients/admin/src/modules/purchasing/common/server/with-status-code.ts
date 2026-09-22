import "server-only";

import { HttpError } from "@/lib/server/http";
import type { Result, ResultCode } from "@/types/api";

/**
 * `guardCall` collapses every non-400/401 HTTP failure into code "error", which loses the distinction
 * callers need (404 not found; 409 deterministic refusal vs. an ambiguous network failure). A definitive
 * 4xx status is mapped back to the backend's own result code here; anything else (network error, timeout,
 * 5xx, non-JSON) is rethrown so `guardCall` reports "error". Mirrors modules/transfers/api/transfers.api.ts.
 * Server-only, so it is imported directly by the Purchasing `*.api.ts` files rather than through the
 * client-safe `common` barrel.
 */
const CODE_BY_STATUS: Record<number, ResultCode> = {
  400: "bad_request",
  401: "unauthorized",
  403: "forbidden",
  404: "not_found",
  409: "conflict",
};

export async function withStatusCode<T>(call: () => Promise<Result<T>>): Promise<Result<T>> {
  try {
    return await call();
  } catch (error) {
    const code = error instanceof HttpError ? CODE_BY_STATUS[error.status] : undefined;
    if (!code) throw error;
    return {
      requestId: "",
      code,
      isSuccess: false,
      message: (error as HttpError).message,
      data: null as T,
    };
  }
}

/** Result codes that mean the backend deterministically refused the request (as opposed to an ambiguous failure). */
export const DEFINITIVE_CODES: ReadonlySet<string> = new Set(["bad_request", "conflict", "not_found", "forbidden"]);
