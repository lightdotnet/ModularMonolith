import "server-only";

import { getSession } from "@/lib/server/session";
import type { HttpRequestHandler } from "@/lib/server/http";

export const bearerTokenHandler: HttpRequestHandler = async (context) => {
  const session = await getSession();
  if (session?.accessToken) {
    context.headers.Authorization = `Bearer ${session.accessToken}`;
  }
};

export function explicitBearerTokenHandler(accessToken: string): HttpRequestHandler {
  return (context) => {
    context.headers.Authorization = `Bearer ${accessToken}`;
  };
}
