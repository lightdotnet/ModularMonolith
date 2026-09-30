import "server-only";

import { HttpError } from "@/lib/server/http";
import type { ApiResponse, Result, ResultCode } from "@/types/api";

function describeError(error: unknown): string {
  return error instanceof Error
    ? error.message
    : "Unexpected error while calling the API.";
}

/** Maps a thrown `HttpError`'s status to the backend's own `ResultCode` vocabulary; anything else (network failure, timeout, non-JSON body) is just "error". */
function codeFromError(error: unknown): ResultCode {
  if (error instanceof HttpError) {
    if (error.status === 401) return "unauthorized";
    if (error.status === 400) return "bad_request";
  }
  return "error";
}

function errorResponse(error: unknown): ApiResponse {
  return {
    requestId: "",
    code: codeFromError(error),
    isSuccess: false,
    message: describeError(error),
  };
}

/**
 * Runs a backend call and normalizes any thrown error (network failure,
 * non-JSON response, timeout, ...) into the same `Result` envelope the
 * backend itself returns, so callers only ever handle one shape.
 */
export async function guardCall<T>(
  call: () => Promise<Result<T>>,
): Promise<Result<T | null>> {
  try {
    return await call();
  } catch (error) {
    return { ...errorResponse(error), data: null };
  }
}

/** Same as `guardCall`, for endpoints whose success response carries no `data`. */
export async function guardResponseCall(
  call: () => Promise<ApiResponse>,
): Promise<ApiResponse> {
  try {
    return await call();
  } catch (error) {
    return errorResponse(error);
  }
}

/** For endpoints that return a bare value/array instead of a `Result` envelope. */
export async function guardRawCall<T>(
  call: () => Promise<T>,
): Promise<
  { isSuccess: true; data: T } | { isSuccess: false; data: null; message: string }
> {
  try {
    const data = await call();
    return { isSuccess: true, data };
  } catch (error) {
    return { isSuccess: false, data: null, message: describeError(error) };
  }
}
