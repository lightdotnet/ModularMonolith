"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { forcePassword } from "@/modules/identity/users/api/users.api";

export interface ForcePasswordFormState {
  error?: string;
  success?: boolean;
}

export async function forcePasswordAction(
  _prevState: ForcePasswordFormState,
  formData: FormData,
): Promise<ForcePasswordFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const id = String(formData.get("id") ?? "").trim();
  const newPassword = String(formData.get("newPassword") ?? "");

  if (!id || !newPassword) {
    return { error: "A new password is required." };
  }

  const result = await forcePassword(id, newPassword);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to reset password." };
  }

  return { success: true };
}
