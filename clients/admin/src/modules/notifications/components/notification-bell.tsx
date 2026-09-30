"use client";

import { useState } from "react";
import Link from "next/link";
import { Bell } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { cn } from "@/lib/shared/utils";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Empty,
  EmptyDescription,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useNotificationsContext } from "@/modules/notifications/context/notifications-provider";
import { NotificationStatus } from "@/modules/notifications/types/notification";

type NotificationFilter = "all" | "unread" | "archived";

// Filtering happens server-side (via `refresh`) rather than over the already-fetched
// batch — the badge count is a true total across all pages, so a client-side filter
// over just the latest page could show fewer items than the badge implies.
const FILTER_TO_STATUS: Record<NotificationFilter, NotificationStatus | undefined> = {
  all: undefined,
  unread: NotificationStatus.None,
  archived: NotificationStatus.Archived,
};

const EMPTY_MESSAGE: Record<NotificationFilter, string> = {
  all: "You're all caught up.",
  unread: "No unread notifications.",
  archived: "No archived notifications.",
};

export function NotificationBell() {
  const { notifications, unreadCount, markAsRead, refresh } = useNotificationsContext();
  const [filter, setFilter] = useState<NotificationFilter>("all");

  function handleFilterChange(value: string) {
    const next = value as NotificationFilter;
    setFilter(next);
    void refresh(FILTER_TO_STATUS[next]);
  }

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="relative"
          aria-label="Notifications"
        >
          <Bell
            className={cn(
              unreadCount > 0 &&
                "origin-top [animation:bell-ring_1.5s_ease-in-out_infinite] motion-reduce:animate-none",
            )}
          />
          {unreadCount > 0 && (
            <Badge
              variant="destructive"
              className="absolute -top-1 -right-1 h-4 min-w-4 justify-center rounded-full px-1 py-0 text-[0.65rem] leading-none"
            >
              <span className="translate-y-px">{unreadCount > 99 ? "99+" : unreadCount}</span>
            </Badge>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-72">
        <DropdownMenuLabel>Notifications</DropdownMenuLabel>
        <DropdownMenuSeparator />

        <Tabs value={filter} onValueChange={handleFilterChange}>
          <TabsList className="mx-1.5 mb-1 w-full" style={{ width: "calc(100% - 0.75rem)" }}>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="unread">Unread</TabsTrigger>
            <TabsTrigger value="archived">Archived</TabsTrigger>
          </TabsList>
        </Tabs>

        <div className="max-h-72 overflow-y-auto">
          {notifications.length === 0 ? (
            <Empty className="p-4">
              <EmptyMedia variant="icon">
                <Bell />
              </EmptyMedia>
              <EmptyTitle>No notifications</EmptyTitle>
              <EmptyDescription>{EMPTY_MESSAGE[filter]}</EmptyDescription>
            </Empty>
          ) : (
            notifications.map((notification) => {
              const content = (
                <div className="flex w-full flex-col gap-0.5">
                  <div className="flex items-center gap-2">
                    {notification.status === NotificationStatus.None && (
                      <span
                        className="size-1.5 shrink-0 rounded-full bg-primary"
                        aria-hidden
                      />
                    )}
                    <span className="text-sm font-medium text-foreground">
                      {notification.title}
                    </span>
                  </div>
                  {notification.message && (
                    <span className="text-xs text-muted-foreground">
                      {notification.message}
                    </span>
                  )}
                  <LocalDateTime
                    value={notification.created}
                    className="text-[0.65rem] text-muted-foreground"
                  />
                </div>
              );

              return notification.url ? (
                <DropdownMenuItem key={notification.id} asChild>
                  <Link
                    href={notification.url}
                    className="flex-col items-start py-2"
                    onClick={() => void markAsRead(notification.id)}
                  >
                    {content}
                  </Link>
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem
                  key={notification.id}
                  className="flex-col items-start py-2"
                  onClick={() => void markAsRead(notification.id)}
                >
                  {content}
                </DropdownMenuItem>
              );
            })
          )}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
