"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { markNotificationRead } from "@/modules/notifications/api/user-notifications.api";

export async function markNotificationReadAction(id: string): Promise<boolean> {
  const session = await resolveSession();
  if (!session) return false;

  const result = await markNotificationRead(id);
  return result.isSuccess;
}
