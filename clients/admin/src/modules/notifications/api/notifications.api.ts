import { notificationsApi } from "@/lib/server/backend-api";

const { requestJson } = notificationsApi;
import { guardResponseCall, guardCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult } from "@/types/api";
import type {
  NotificationDto,
  NotificationLookupParams,
  SendNotificationRequest,
} from "@/modules/notifications/types/notification";

export function getNotifications(params: NotificationLookupParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<NotificationDto>>("notification", {
      method: "GET",
      query: {
        toUserId: params.toUserId,
        status: params.status !== undefined ? String(params.status) : undefined,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 10),
      },
    }),
  );
}

export function sendNotification(request: SendNotificationRequest) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>("notification", {
      method: "POST",
      query: {
        fromUserId: request.fromUserId,
        fromName: request.fromName ?? undefined,
        toUserId: request.toUserId,
      },
      body: {
        title: request.title,
        message: request.message,
        url: request.url,
        byMessage: false,
      },
    }),
  );
}
