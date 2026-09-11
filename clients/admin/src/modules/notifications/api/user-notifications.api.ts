import { notificationsApi } from "@/lib/server/backend-api";

const { requestJson } = notificationsApi;
import { guardCall } from "@/lib/server/call-guard";
import type { Result, PagedResult } from "@/types/api";
import type {
  NotificationDto,
  NotificationLookupParams,
} from "@/modules/notifications/types/notification";

export function getMyNotifications(
  params: Omit<NotificationLookupParams, "toUserId"> = {},
) {
  return guardCall(() =>
    requestJson<PagedResult<NotificationDto>>("user_notification", {
      method: "GET",
      query: {
        status: params.status !== undefined ? String(params.status) : undefined,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 10),
      },
    }),
  );
}

export function getUnreadCount() {
  return guardCall(() =>
    requestJson<Result<number>>("user_notification/count_unread"),
  );
}

/** `Get(entryId)` marks the entry as read as a side effect and returns the updated record. */
export function markNotificationRead(id: string) {
  return guardCall(() =>
    requestJson<Result<NotificationDto | null>>(`user_notification/${id}`),
  );
}
