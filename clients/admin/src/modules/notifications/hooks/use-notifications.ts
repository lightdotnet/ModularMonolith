"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  HttpTransportType,
  HubConnectionBuilder,
  LogLevel,
  type HubConnection,
} from "@microsoft/signalr";
import { getMyNotificationsAction } from "@/modules/notifications/api/get-my-notifications-action";
import { getUnreadCountAction } from "@/modules/notifications/api/get-unread-count-action";
import { markNotificationReadAction } from "@/modules/notifications/api/mark-notification-read-action";
import { getSignalRTokenAction } from "@/modules/notifications/api/get-signalr-token-action";
import {
  NotificationStatus,
  type NotificationDto,
} from "@/modules/notifications/types/notification";

// SignalR embeds the raw negotiate response body in the error message. When
// the URL is misconfigured, that body is an HTML error page instead of
// JSON/XML — strip it so the console isn't flooded with a full HTML document.
function sanitizeSignalRErrorMessage(message: string): string {
  const htmlIndex = message.search(/<!DOCTYPE html|<html[\s>]/i);
  return htmlIndex === -1 ? message : message.slice(0, htmlIndex).trim();
}

export function useNotifications() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const connectionRef = useRef<HubConnection | null>(null);
  // Remembers the last status filter so a SignalR push re-fetches the tab
  // currently being viewed instead of resetting it back to "all".
  const statusRef = useRef<NotificationStatus | undefined>(undefined);

  const refresh = useCallback(async (status?: NotificationStatus) => {
    statusRef.current = status;
    const [listResult, count] = await Promise.all([
      getMyNotificationsAction({ status }),
      getUnreadCountAction(),
    ]);
    if (listResult.data) setNotifications(listResult.data.records);
    setUnreadCount(count);
  }, []);

  const markAsRead = useCallback(async (id: string) => {
    const success = await markNotificationReadAction(id);
    if (!success) return false;

    setNotifications((previous) =>
      previous.map((notification) =>
        notification.id === id
          ? { ...notification, status: NotificationStatus.Read }
          : notification,
      ),
    );
    setUnreadCount(await getUnreadCountAction());
    return true;
  }, []);

  useEffect(() => {
    let cancelled = false;
    let retryTimeout: ReturnType<typeof setTimeout> | undefined;

    const connectSignalR = async () => {
      const tokenState = await getSignalRTokenAction();
      if (cancelled || !tokenState) return;

      const connection = new HubConnectionBuilder()
        .withUrl(tokenState.hubUrl, {
          // Re-mint on every (re)connect instead of capturing once: SignalR
          // awaits an async accessTokenFactory and invokes it again for each
          // negotiate, so withAutomaticReconnect() gets a fresh short-lived
          // hub token rather than reusing a now-expired one.
          accessTokenFactory: async () =>
            (await getSignalRTokenAction())?.accessToken ?? "",
          transport: HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect()
        // Navigating away (e.g. to a 404, which unmounts the whole dashboard
        // layout) intentionally stops this connection mid-flight, which the
        // browser surfaces as an abnormal WS closure (code 1006). SignalR logs
        // that at Error level by default; our own connect-failure logging
        // below stays untouched since it's an explicit console.error call.
        .configureLogging(LogLevel.Critical)
        .build();

      // Payload is the raw `SystemMessage` pushed by the backend (no id/status/created),
      // so re-fetch instead of merging a partial one.
      connection.on("SystemMessage", () => void refresh(statusRef.current));

      try {
        await connection.start();
      } catch (error) {
        console.error(
          error instanceof Error
            ? sanitizeSignalRErrorMessage(error.message)
            : error,
        );
        retryTimeout = setTimeout(() => void connectSignalR(), 30_000);
        return;
      }

      if (cancelled) {
        void connection.stop();
        return;
      }
      connectionRef.current = connection;
    };

    void (async () => {
      await refresh();
      if (cancelled) return;
      setLoading(false);
      await connectSignalR();
    })();

    return () => {
      cancelled = true;
      if (retryTimeout) clearTimeout(retryTimeout);
      void connectionRef.current?.stop();
      connectionRef.current = null;
    };
  }, [refresh]);

  return { notifications, unreadCount, loading, markAsRead, refresh };
}
