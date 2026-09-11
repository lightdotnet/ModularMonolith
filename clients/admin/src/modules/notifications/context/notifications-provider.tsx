"use client";

import { createContext, useContext, type ReactNode } from "react";
import { useNotifications } from "@/modules/notifications/hooks/use-notifications";

type NotificationsContextValue = ReturnType<typeof useNotifications>;

const NotificationsContext = createContext<NotificationsContextValue | null>(null);

/**
 * Mounted once at the shell level so the topbar bell and any other consumer
 * (e.g. the home page's inbox) share a single live connection and `unreadCount` —
 * marking a notification read from either place updates both immediately.
 */
export function NotificationsProvider({ children }: { children: ReactNode }) {
  const value = useNotifications();
  return (
    <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>
  );
}

export function useNotificationsContext() {
  const context = useContext(NotificationsContext);
  if (!context) {
    throw new Error("useNotificationsContext must be used within a NotificationsProvider");
  }
  return context;
}
