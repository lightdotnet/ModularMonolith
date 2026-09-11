"use server";

import { redirect } from "next/navigation";
import { getToken } from "@/modules/identity/auth/api/token.api";
import { establishSession } from "@/modules/identity/auth/api/establish-session";

export interface LoginFormState {
  error?: string;
}

export async function loginAction(
  _prevState: LoginFormState,
  formData: FormData,
): Promise<LoginFormState> {
  const username = String(formData.get("username") ?? "");
  const password = String(formData.get("password") ?? "");

  if (!username || !password) {
    return { error: "Username and password are required." };
  }

  const tokenResult = await getToken({ username, password });

  if (!tokenResult.isSuccess || !tokenResult.data) {
    return { error: tokenResult.message || "Login failed." };
  }

  await establishSession(tokenResult.data);

  const redirectTo = String(formData.get("redirect") ?? "");
  // Only allow same-site relative paths — reject "//host/..." to avoid an open redirect.
  const destination =
    redirectTo.startsWith("/") && !redirectTo.startsWith("//") ? redirectTo : "/";

  redirect(destination);
}
