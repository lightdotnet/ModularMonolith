"use server";

import { revalidatePath } from "next/cache";
import { resolveSession } from "@/modules/identity/user-profile";
import { sendNotification } from "@/modules/notifications/api/notifications.api";

export interface SendNotificationFormState {
  error?: string;
  success?: boolean;
}

export async function sendNotificationAction(
  _prevState: SendNotificationFormState,
  formData: FormData,
): Promise<SendNotificationFormState> {
  const session = await resolveSession();
  if (!session || !session.profile) {
    return { error: "Your session has expired. Please sign in again." };
  }

  const toUserId = String(formData.get("toUserId") ?? "").trim();
  const title = String(formData.get("title") ?? "").trim();

  if (!toUserId || !title) {
    return { error: "Recipient and title are required." };
  }

  const result = await sendNotification({
    toUserId,
    title,
    message: String(formData.get("message") ?? "") || undefined,
    url: String(formData.get("url") ?? "") || undefined,
  });

  if (!result.isSuccess) {
    return { error: result.message || "Failed to send notification." };
  }

  revalidatePath("/notifications");
  return { success: true };
}
