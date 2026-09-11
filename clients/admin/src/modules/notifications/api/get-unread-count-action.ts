"use server";

import { resolveSession } from "@/modules/identity/user-profile";
import { getUnreadCount } from "@/modules/notifications/api/user-notifications.api";

export async function getUnreadCountAction(): Promise<number> {
  const session = await resolveSession();
  if (!session) return 0;

  const result = await getUnreadCount();
  if (!result.isSuccess || result.data === null) return 0;

  return result.data;
}
