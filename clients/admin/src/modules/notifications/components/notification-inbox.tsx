"use client";

import { useState, useTransition } from "react";
import { Mail } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import {
  Empty,
  EmptyDescription,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { cn } from "@/lib/shared/utils";
import { getMyNotificationsAction } from "@/modules/notifications/api/get-my-notifications-action";
import { useNotificationsContext } from "@/modules/notifications/context/notifications-provider";
import { NotificationStatus, type NotificationDto } from "@/modules/notifications/types/notification";
import { NotificationList } from "@/modules/notifications/components/notification-list";
import { NotificationDetail } from "@/modules/notifications/components/notification-detail";

type NotificationFilter = "all" | "unread" | "archived";

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

interface NotificationInboxProps {
  initialNotifications: NotificationDto[];
  initialTotalPages: number;
}

export function NotificationInbox({
  initialNotifications,
  initialTotalPages,
}: NotificationInboxProps) {
  const { unreadCount, markAsRead } = useNotificationsContext();
  const [notifications, setNotifications] = useState(initialNotifications);
  const [totalPages, setTotalPages] = useState(initialTotalPages);
  const [pageNumber, setPageNumber] = useState(1);
  const [filter, setFilter] = useState<NotificationFilter>("all");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function loadPage(nextPage: number, nextFilter: NotificationFilter) {
    startTransition(async () => {
      const result = await getMyNotificationsAction({
        pageNumber: nextPage,
        status: FILTER_TO_STATUS[nextFilter],
      });
      setNotifications(result.data?.records ?? []);
      setTotalPages(result.data?.totalPages ?? 1);
      setPageNumber(nextPage);
      setSelectedId(null);
    });
  }

  function handleFilterChange(value: string) {
    const next = value as NotificationFilter;
    setFilter(next);
    loadPage(1, next);
  }

  async function handleSelect(notification: NotificationDto) {
    setSelectedId(notification.id);
    if (notification.status !== NotificationStatus.None) return;

    const success = await markAsRead(notification.id);
    if (!success) return;

    setNotifications((previous) =>
      previous.map((item) =>
        item.id === notification.id ? { ...item, status: NotificationStatus.Read } : item,
      ),
    );
  }

  const selected = notifications.find((notification) => notification.id === selectedId) ?? null;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between gap-2">
        <Tabs value={filter} onValueChange={handleFilterChange}>
          <TabsList>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="unread">Unread</TabsTrigger>
            <TabsTrigger value="archived">Archived</TabsTrigger>
          </TabsList>
        </Tabs>
        {unreadCount > 0 && <Badge variant="destructive">{unreadCount} unread</Badge>}
      </div>

      {notifications.length === 0 ? (
        <Empty className="min-h-80">
          <EmptyMedia variant="icon">
            <Mail />
          </EmptyMedia>
          <EmptyTitle>No notifications</EmptyTitle>
          <EmptyDescription>{EMPTY_MESSAGE[filter]}</EmptyDescription>
        </Empty>
      ) : (
        <div
          className={cn(
            "grid grid-cols-1 overflow-hidden rounded-2xl border border-border transition-opacity md:h-[28rem] md:grid-cols-[minmax(0,320px)_1fr]",
            isPending && "opacity-60",
          )}
        >
          <NotificationList
            notifications={notifications}
            selectedId={selectedId}
            onSelect={handleSelect}
          />
          <div className="border-t border-border md:border-t-0 md:border-l">
            <NotificationDetail notification={selected} />
          </div>
        </div>
      )}

      {totalPages > 1 && (
        <Pagination>
          <PaginationContent>
            <PaginationItem>
              <PaginationPrevious
                href="#"
                aria-disabled={pageNumber <= 1}
                className={pageNumber <= 1 ? "pointer-events-none opacity-50" : undefined}
                onClick={(event) => {
                  event.preventDefault();
                  if (pageNumber > 1) loadPage(pageNumber - 1, filter);
                }}
              />
            </PaginationItem>
            <PaginationItem>
              <span className="px-3 text-sm text-muted-foreground">
                Page {pageNumber} of {totalPages}
              </span>
            </PaginationItem>
            <PaginationItem>
              <PaginationNext
                href="#"
                aria-disabled={pageNumber >= totalPages}
                className={pageNumber >= totalPages ? "pointer-events-none opacity-50" : undefined}
                onClick={(event) => {
                  event.preventDefault();
                  if (pageNumber < totalPages) loadPage(pageNumber + 1, filter);
                }}
              />
            </PaginationItem>
          </PaginationContent>
        </Pagination>
      )}
    </div>
  );
}
