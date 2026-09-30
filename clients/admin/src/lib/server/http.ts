import "server-only";

import { getApiBaseUrl } from "@/lib/server/config";
import { type ApiClientName } from "@/lib/server/api-clients";

/** Thrown when a backend response is non-2xx; carries the HTTP status so callers can distinguish permanent (401/400) from transient (5xx/network) failures. */
export class HttpError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "HttpError";
  }
}

export interface HttpRequestContext {
  headers: Record<string, string>;
}

export type HttpRequestHandler = (context: HttpRequestContext) => Promise<void> | void;

export interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  query?: Record<string, string | undefined>;
  body?: unknown;
  handlers?: HttpRequestHandler[];
  client: ApiClientName;
}

function buildUrl(
  path: string,
  query: Record<string, string | undefined> | undefined,
  client: ApiClientName,
): URL {
  const url = new URL(path, getApiBaseUrl(client));
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined) url.searchParams.set(key, value);
    }
  }
  return url;
}

/**
 * Non-2xx responses can carry a real reason in the body — this app's own
 * `ApiResponse.message` envelope, an ASP.NET `ValidationProblemDetails.errors`
 * map, or a `ProblemDetails.title` — prefer whichever is present over the
 * generic status-code message.
 */
async function extractErrorMessage(response: Response, path: string): Promise<string> {
  try {
    const body = await response.clone().json();
    if (typeof body?.message === "string" && body.message) return body.message;
    if (body?.errors && typeof body.errors === "object") {
      const firstMessage = Object.values(body.errors)
        .flat()
        .find((message) => typeof message === "string");
      if (firstMessage) return firstMessage as string;
    }
    if (typeof body?.title === "string" && body.title) return body.title;
  } catch {
    // Non-JSON body — fall through to the generic message.
  }

  return `Backend request to ${path} failed with status ${response.status}.`;
}

/** Dev-only request tracing — never logs bodies, query, or headers (they carry bearer tokens). */
function logApi(message: string): void {
  if (process.env.NODE_ENV !== "development") return;
  console.log(`[api] ${message}`);
}

/**
 * Node's `fetch` reports network/DNS/TLS failures as a generic
 * `TypeError: fetch failed` and hides the real reason on `error.cause`
 * (e.g. `DEPTH_ZERO_SELF_SIGNED_CERT`, `ECONNREFUSED`) — pull it back out.
 */
function extractCause(error: unknown): string | undefined {
  if (!(error instanceof Error) || !(error.cause instanceof Error)) return undefined;
  const causeCode = (error.cause as unknown as { code?: string }).code;
  return causeCode ?? error.cause.message;
}

async function send(path: string, options: RequestOptions): Promise<Response> {
  const context: HttpRequestContext = {
    headers: { "Content-Type": "application/json" },
  };
  for (const handler of options.handlers ?? []) {
    await handler(context);
  }

  const method = options.method ?? "GET";
  logApi(`→ ${method} ${options.client} ${path}`);

  let response: Response;
  try {
    response = await fetch(buildUrl(path, options.query, options.client), {
      method,
      headers: context.headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch (error) {
    const cause = extractCause(error);
    logApi(`✗ ${method} ${path} — fetch failed ${cause ? `(cause: ${cause})` : ""}`);
    throw new Error(
      cause ? `fetch failed: ${cause}` : error instanceof Error ? error.message : "fetch failed",
      { cause: error },
    );
  }

  if (!response.ok) {
    const message = await extractErrorMessage(response, path);
    logApi(`✗ ${response.status} ${method} ${path} — ${message}`);
    throw new HttpError(response.status, message);
  }

  logApi(`← ${response.status} ${method} ${path}`);
  return response;
}

export async function requestJson<T>(
  path: string,
  options: RequestOptions,
): Promise<T> {
  const response = await send(path, options);
  return (await response.json()) as T;
}

export async function requestVoid(
  path: string,
  options: RequestOptions,
): Promise<void> {
  await send(path, options);
}
