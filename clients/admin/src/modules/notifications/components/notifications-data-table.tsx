"use client";

import { useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Bell as BellIcon, Send } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { NativeSelect } from "@/components/ui/native-select";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { SendNotificationDialog } from "@/modules/notifications/components/send-notification-dialog";
import { UserSelect } from "@/modules/notifications/components/user-select";
import { NotificationStatus, type NotificationDto } from "@/modules/notifications/types/notification";

interface NotificationsDataTableProps {
  records: NotificationDto[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalRecords: number;
  error?: DataTableErrorState;
  canSend?: boolean;
  toUserId?: string;
  status?: NotificationStatus;
}

const STATUS_FILTER_OPTIONS = [
  { value: NotificationStatus.None, label: "Unread" },
  { value: NotificationStatus.Read, label: "Read" },
  { value: NotificationStatus.Archived, label: "Archived" },
];

const STATUS_LABEL: Record<NotificationStatus, string> = {
  [NotificationStatus.None]: "Unread",
  [NotificationStatus.Read]: "Read",
  [NotificationStatus.Archived]: "Archived",
};

const STATUS_VARIANT: Record<NotificationStatus, "destructive" | "secondary" | "outline"> = {
  [NotificationStatus.None]: "destructive",
  [NotificationStatus.Read]: "secondary",
  [NotificationStatus.Archived]: "outline",
};

export function NotificationsDataTable({
  records,
  pageNumber,
  pageSize,
  totalPages,
  totalRecords,
  error,
  canSend,
  toUserId,
  status,
}: NotificationsDataTableProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [sendOpen, setSendOpen] = useState(false);
  const [sendDialogKey, setSendDialogKey] = useState(0);

  // Pending filter state — only applied to the URL when "Search" is clicked,
  // not as each field changes. Re-synced from props if the URL changes externally
  // (e.g. browser back/forward), mirroring DataTableToolbar's own search-sync pattern.
  const [pendingStatus, setPendingStatus] = useState(status ?? "");
  const [lastStatus, setLastStatus] = useState(status ?? "");
  if ((status ?? "") !== lastStatus) {
    setLastStatus(status ?? "");
    setPendingStatus(status ?? "");
  }

  const [pendingToUserId, setPendingToUserId] = useState(toUserId ?? "");
  const [lastToUserId, setLastToUserId] = useState(toUserId ?? "");
  if ((toUserId ?? "") !== lastToUserId) {
    setLastToUserId(toUserId ?? "");
    setPendingToUserId(toUserId ?? "");
  }

  function navigate(nextParams: Record<string, string | undefined>) {
    const params = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(nextParams)) {
      if (value) params.set(key, value);
      else params.delete(key);
    }

    startTransition(() => {
      router.push(`${pathname}?${params.toString()}`);
    });
  }

  const actions: DataTableAction[] | undefined = canSend
    ? [
        {
          key: "send",
          label: "Send notification",
          icon: Send,
          onClick: () => {
            setSendDialogKey((key) => key + 1);
            setSendOpen(true);
          },
        },
      ]
    : undefined;

  const columns: DataTableColumn<NotificationDto>[] = [
    {
      id: "message",
      header: "Message",
      cell: (notification) => (
        <div>
          <p className="font-medium">{notification.title}</p>
          <div className="flex items-start justify-end">
            <LocalDateTime
              value={notification.created}
              className="text-muted-foreground text-right text-sm"
            />
          </div>
          <textarea
            className="mt-1 whitespace-pre-wrap text-muted-foreground w-full"
            value={notification.message ?? ""}
            disabled
            rows={3}
          />
        </div>
      ),
      className: "w-[80%]",
    },
    {
      id: "status",
      header: "Status",
      cell: (notification) => (
        <Badge variant={STATUS_VARIANT[notification.status]}>
          {STATUS_LABEL[notification.status]}
        </Badge>
      ),
    },
    {
      id: "from",
      header: "From",
      cell: (notification) => notification.fromName ?? notification.fromUserId,
    },
  ];

  const customSearch = (
    <>
      <NativeSelect
        className="w-40"
        aria-label="Filter by status"
        placeholder="All statuses"
        value={pendingStatus}
        onChange={setPendingStatus}
        options={STATUS_FILTER_OPTIONS}
      />

      <UserSelect
        value={pendingToUserId}
        onValueChange={(user) => setPendingToUserId(user.id)}
        triggerClassName="w-56"
        ariaLabel="Filter by recipient"
        placeholder="Search recipients"
        onClear={() => setPendingToUserId("")}
      />
    </>
  );

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(notification) => notification.id}
        isLoading={isPending}
        actions={actions}
        customSearch={customSearch}
        onCustomSearch={() =>
          navigate({
            status: pendingStatus === "" ? undefined : pendingStatus,
            toUserId: pendingToUserId === "" ? undefined : pendingToUserId,
            page: undefined,
          })
        }
        onRefresh={() => startTransition(() => router.refresh())}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalRecords={totalRecords}
        onPageChange={(page) => navigate({ page: String(page) })}
        error={error}
        emptyState={{
          icon: BellIcon,
          title: "No notifications found",
          description: "Nothing has been sent yet.",
        }}
      />
      {canSend && (
        <SendNotificationDialog
          key={`send-${sendDialogKey}`}
          open={sendOpen}
          onOpenChange={setSendOpen}
          onSent={() => router.refresh()}
        />
      )}
    </>
  );
}
