"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { ALL_SESSION_COOKIE_NAMES } from "@/lib/server/session-cookie";

export async function logoutAction(redirectTo?: string): Promise<void> {
  const cookieStore = await cookies();
  for (const name of ALL_SESSION_COOKIE_NAMES) cookieStore.delete(name);

  // Only allow same-site relative paths — reject "//host/..." to avoid an open redirect.
  const isSafe = !!redirectTo && redirectTo.startsWith("/") && !redirectTo.startsWith("//");
  redirect(isSafe ? `/login?redirect=${encodeURIComponent(redirectTo)}` : "/login");
}
