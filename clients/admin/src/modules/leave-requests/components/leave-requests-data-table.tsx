"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { CalendarDays, Eye, Pencil, Plus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { CreateLeaveRequestDialog } from "@/modules/leave-requests/components/create-leave-request-dialog";
import { EditLeaveRequestDialog } from "@/modules/leave-requests/components/edit-leave-request-dialog";
import { DeleteLeaveRequestDialog } from "@/modules/leave-requests/components/delete-leave-request-dialog";
import { LEAVE_REQUEST_STATUS_VARIANT } from "@/modules/leave-requests/constants/status-variant";
import { LeaveRequestStatus, type LeaveRequestDto } from "@/modules/leave-requests/types/leave-request";

interface LeaveRequestsDataTableProps {
  records: LeaveRequestDto[];
  error?: DataTableErrorState;
  currentUserId: string;
  /** Whether the viewer holds `leave.requests.manage` — threaded through to `EditLeaveRequestDialog`
   * so it knows whether to show the approver picker (only self-service edits resubmit to Approval). */
  canManage: boolean;
  /** `"mine"` shows Edit+Delete only for the viewer's own not-yet-approved requests (the normal
   * self-service rule). `"all"` (manager view of every employee's requests) shows Delete only,
   * for every row, regardless of owner/status — manage does not include editing someone else's
   * request. */
  variant: "mine" | "all";
  /** employeeId -> display name, resolved once by the page. Only rendered as a column for `"all"`. */
  employeeNamesById?: Map<string, string>;
}

const EDITABLE_STATUSES = new Set([LeaveRequestStatus.Pending, LeaveRequestStatus.Rejected]);

export function LeaveRequestsDataTable({
  records,
  error,
  currentUserId,
  canManage,
  variant,
  employeeNamesById,
}: LeaveRequestsDataTableProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [selected, setSelected] = useState<LeaveRequestDto | null>(null);

  function onRefresh() {
    startTransition(() => router.refresh());
  }

  function canEdit(row: LeaveRequestDto) {
    return variant === "mine" && row.userId === currentUserId && EDITABLE_STATUSES.has(row.status);
  }

  function canDelete(row: LeaveRequestDto) {
    if (variant === "all") return true;
    return row.userId === currentUserId && EDITABLE_STATUSES.has(row.status);
  }

  const actions: DataTableAction[] =
    variant === "mine"
      ? [
          {
            key: "create",
            label: "New leave request",
            icon: Plus,
            onClick: () => {
              setCreateDialogKey((key) => key + 1);
              setCreateOpen(true);
            },
          },
        ]
      : [];

  const columns: DataTableColumn<LeaveRequestDto>[] = [
    ...(variant === "all"
      ? [
          {
            id: "employee",
            header: "Employee",
            cell: (row: LeaveRequestDto) => employeeNamesById?.get(row.employeeId) ?? row.employeeId,
          } satisfies DataTableColumn<LeaveRequestDto>,
        ]
      : []),
    {
      id: "leaveType",
      header: "Type",
      cell: (row) => <span className="font-medium">{row.leaveType}</span>,
    },
    {
      id: "startDate",
      header: "Start",
      cell: (row) => <LocalDateTime value={row.startDate} />,
    },
    {
      id: "endDate",
      header: "End",
      cell: (row) => <LocalDateTime value={row.endDate} />,
    },
    {
      id: "reason",
      header: "Reason",
      cell: (row) => (
        <p className="max-w-xs truncate text-sm text-muted-foreground">{row.reason}</p>
      ),
    },
    {
      id: "status",
      header: "Status",
      cell: (row) => <Badge variant={LEAVE_REQUEST_STATUS_VARIANT[row.status]}>{row.status}</Badge>,
    },
    {
      id: "created",
      header: "Created",
      cell: (row) => <LocalDateTime value={row.created} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (row) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button aria-label="Row actions" size="icon" variant="outline">
              <Pencil />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => router.push(`/leave-requests/${row.id}`)}>
              <Eye className="size-4" />
              View
            </DropdownMenuItem>
            {canEdit(row) && (
              <DropdownMenuItem
                onClick={() => {
                  setSelected(row);
                  setEditOpen(true);
                }}
              >
                Edit
              </DropdownMenuItem>
            )}
            {canDelete(row) && (
              <DropdownMenuItem
                onClick={() => {
                  setSelected(row);
                  setDeleteOpen(true);
                }}
                variant="destructive"
              >
                Delete
              </DropdownMenuItem>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ];

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(row) => row.id}
        isLoading={isPending}
        actions={actions}
        onRefresh={onRefresh}
        pageNumber={1}
        pageSize={Math.max(records.length, 1)}
        totalPages={1}
        totalRecords={records.length}
        onPageChange={() => {}}
        error={error}
        emptyState={{
          icon: CalendarDays,
          title: "No leave requests",
          description:
            variant === "mine"
              ? "Submit a leave request to see it here."
              : "No leave requests have been submitted yet.",
        }}
      />
      {variant === "mine" && (
        <CreateLeaveRequestDialog
          key={`create-${createDialogKey}`}
          open={createOpen}
          onOpenChange={setCreateOpen}
          onCreated={onRefresh}
        />
      )}
      <EditLeaveRequestDialog
        open={editOpen}
        onOpenChange={setEditOpen}
        leaveRequest={selected}
        onUpdated={onRefresh}
        canManage={canManage}
      />
      <DeleteLeaveRequestDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        leaveRequest={selected}
        onDeleted={onRefresh}
      />
    </>
  );
}
