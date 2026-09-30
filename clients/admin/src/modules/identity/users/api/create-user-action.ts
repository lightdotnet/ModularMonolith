"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { createUser } from "@/modules/identity/users/api/users.api";
import type { CreateUserRequest } from "@/modules/identity/users/types/user";

export interface CreateUserFormState {
  error?: string;
  success?: boolean;
}

export async function createUserAction(
  _prevState: CreateUserFormState,
  formData: FormData,
): Promise<CreateUserFormState> {
  const session = await resolveSession();
  if (!session) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const userName = String(formData.get("new-userName") ?? "").trim();
  const password = String(formData.get("new-password") ?? "");
  const authProvider = String(formData.get("authProvider") ?? "") || undefined;

  if (!userName || (!password && authProvider !== "AD")) {
    return { error: "Username and password are required." };
  }

  const request: CreateUserRequest = {
    userName,
    password,
    firstName: String(formData.get("firstName") ?? "") || undefined,
    lastName: String(formData.get("lastName") ?? "") || undefined,
    email: String(formData.get("email") ?? "") || undefined,
    phoneNumber: String(formData.get("phoneNumber") ?? "") || undefined,
    authProvider,
  };

  const result = await createUser(request);

  if (!result.isSuccess) {
    return { error: result.message || "Failed to create user." };
  }

  revalidatePath("/identity/users");
  return { success: true };
}
