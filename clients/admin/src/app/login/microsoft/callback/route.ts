import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { establishSession, exchangeExternalLoginCode } from "@/modules/identity/auth";
import {
  EXTERNAL_LOGIN_PKCE_COOKIE_NAME,
  EXTERNAL_LOGIN_PKCE_COOKIE_PATH,
} from "@/lib/server/external-login-pkce";

/** Only allow same-site relative paths — reject "//host/..." to avoid an open redirect. */
function sanitizeState(state: string | null): string {
  return state && state.startsWith("/") && !state.startsWith("//") ? state : "/";
}

function loginErrorRedirect(request: NextRequest, message: string): NextResponse {
  const url = new URL("/login", request.url);
  url.searchParams.set("error", message);
  return NextResponse.redirect(url);
}

/**
 * Callback target of the Microsoft external-login handshake
 * (`Identity.Web`'s `ExternalLoginRelayModel`): on `error`, bounces back to
 * `/login` with it. Otherwise redeems the one-time `code` (+ the
 * `code_verifier` stashed by `/login/microsoft/start`) for a token via
 * `Identity.Api`, establishes the session exactly like the password-login
 * flow does, then redirects to the sanitized `state` (the original
 * `returnTo`).
 */
export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;

  const error = searchParams.get("error");
  if (error) {
    const message = searchParams.get("error_description") || "Microsoft sign-in failed.";
    return loginErrorRedirect(request, message);
  }

  const code = searchParams.get("code");
  const state = searchParams.get("state");

  const cookieStore = await cookies();
  const codeVerifier = cookieStore.get(EXTERNAL_LOGIN_PKCE_COOKIE_NAME)?.value;
  cookieStore.delete({ name: EXTERNAL_LOGIN_PKCE_COOKIE_NAME, path: EXTERNAL_LOGIN_PKCE_COOKIE_PATH });

  if (!code || !codeVerifier) {
    return loginErrorRedirect(request, "Your sign-in attempt expired. Please try again.");
  }

  const tokenResult = await exchangeExternalLoginCode(code, codeVerifier);
  if (!tokenResult.isSuccess || !tokenResult.data) {
    return loginErrorRedirect(request, tokenResult.message || "Microsoft sign-in failed.");
  }

  await establishSession(tokenResult.data);

  const destination = sanitizeState(state);
  return NextResponse.redirect(new URL(destination, request.url));
}
