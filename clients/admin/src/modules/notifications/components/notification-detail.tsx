import Link from "next/link";
import { Mail } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { LocalDateTime } from "@/components/shared/local-date-time";
import {
  Empty,
  EmptyDescription,
  EmptyMedia,
  EmptyTitle,
} from "@/components/ui/empty";
import { NotificationStatus, type NotificationDto } from "@/modules/notifications/types/notification";

interface NotificationDetailProps {
  notification: NotificationDto | null;
}

export function NotificationDetail({ notification }: NotificationDetailProps) {
  if (!notification) {
    return (
      <Empty className="h-full">
        <EmptyMedia variant="icon">
          <Mail />
        </EmptyMedia>
        <EmptyTitle>No notification selected</EmptyTitle>
        <EmptyDescription>Choose a notification from the list to read it.</EmptyDescription>
      </Empty>
    );
  }

  return (
    <div className="flex h-full flex-col gap-4 overflow-y-auto p-6">
      <div className="flex flex-col gap-1">
        <div className="flex items-center gap-2">
          <h2 className="text-lg font-semibold text-foreground">{notification.title}</h2>
          {notification.status === NotificationStatus.Archived && (
            <Badge variant="outline">Archived</Badge>
          )}
        </div>
        <p className="text-xs text-muted-foreground">
          {notification.fromName ? `From ${notification.fromName} · ` : ""}
          <LocalDateTime value={notification.created} />
        </p>
      </div>

      {notification.message && (
        <p className="text-sm whitespace-pre-wrap text-foreground">{notification.message}</p>
      )}

      {notification.url && (
        <Link
          href={notification.url}
          className="text-sm font-medium text-primary underline underline-offset-4"
        >
          Open linked page
        </Link>
      )}
    </div>
  );
}
