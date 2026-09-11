import Link from "next/link";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { cn } from "@/lib/shared/utils";
import { NotificationStatus, type NotificationDto } from "@/modules/notifications/types/notification";

interface NotificationListProps {
  notifications: NotificationDto[];
  selectedId: string | null;
  onSelect: (notification: NotificationDto) => void;
}

export function NotificationList({
  notifications,
  selectedId,
  onSelect,
}: NotificationListProps) {
  return (
    <ul className="flex flex-col divide-y divide-border overflow-y-auto md:h-full">
      {notifications.map((notification) => {
        const rowClassName = cn(
          "flex w-full flex-col gap-1 px-4 py-3 text-left transition-colors hover:bg-muted/50",
          selectedId === notification.id && "bg-muted",
        );

        const body = (
          <>
            <div className="flex items-center gap-2">
              {notification.status === NotificationStatus.None && (
                <span className="size-1.5 shrink-0 rounded-full bg-primary" aria-hidden />
              )}
              <span
                className={cn(
                  "truncate text-sm text-foreground",
                  notification.status === NotificationStatus.None && "font-semibold",
                )}
              >
                {notification.title}
              </span>
            </div>
            {notification.fromName && (
              <span className="truncate text-xs text-muted-foreground">
                From {notification.fromName}
              </span>
            )}
            <LocalDateTime
              value={notification.created}
              className="text-[0.65rem] text-muted-foreground"
            />
          </>
        );

        // Only follow app-relative links; anything else stays a plain select row.
        const internalHref = notification.url?.startsWith("/") ? notification.url : null;

        return (
          <li key={notification.id}>
            {internalHref ? (
              <Link href={internalHref} onClick={() => onSelect(notification)} className={rowClassName}>
                {body}
              </Link>
            ) : (
              <button type="button" onClick={() => onSelect(notification)} className={rowClassName}>
                {body}
              </button>
            )}
          </li>
        );
      })}
    </ul>
  );
}
