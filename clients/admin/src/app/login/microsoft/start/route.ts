import { createHash, randomBytes } from "crypto";
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { getIdentityWebBaseUrl } from "@/lib/server/config";
import {
  buildPkceCookieOptions,
  EXTERNAL_LOGIN_PKCE_COOKIE_NAME,
} from "@/lib/server/external-login-pkce";

/** Only allow same-site relative paths — reject "//host/..." to avoid an open redirect. */
function sanitizeReturnTo(returnTo: string | null): string {
  return returnTo && returnTo.startsWith("/") && !returnTo.startsWith("//") ? returnTo : "/";
}

/**
 * Starts the Microsoft external-login handshake: generates a PKCE pair,
 * stashes the `code_verifier` in a short-lived cookie, and does a full
 * top-level redirect to `Identity.Web`'s authorization-code relay
 * (`ExternalLoginStartModel`), which challenges Microsoft OIDC and — on
 * success or failure — redirects the browser back to
 * `/login/microsoft/callback`.
 */
export async function GET(request: NextRequest) {
  const returnTo = sanitizeReturnTo(request.nextUrl.searchParams.get("returnTo"));

  const codeVerifier = randomBytes(32).toString("base64url");
  const codeChallenge = createHash("sha256").update(codeVerifier).digest("base64url");

  const redirectUri = new URL("/login/microsoft/callback", request.url).toString();

  const startUrl = new URL("/Account/ExternalLoginStart", getIdentityWebBaseUrl());
  startUrl.searchParams.set("provider", "Microsoft");
  startUrl.searchParams.set("redirectUri", redirectUri);
  startUrl.searchParams.set("state", returnTo);
  startUrl.searchParams.set("codeChallenge", codeChallenge);

  const response = NextResponse.redirect(startUrl);
  response.cookies.set(EXTERNAL_LOGIN_PKCE_COOKIE_NAME, codeVerifier, buildPkceCookieOptions());
  return response;
}
