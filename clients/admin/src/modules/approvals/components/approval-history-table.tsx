"use client";

import { useState } from "react";
import { History, Plus } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DataTable,
  type DataTableAction,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { ApprovalHistorySheet } from "@/modules/approvals/components/approval-history-sheet";
import { CreateApprovalRequestDialog } from "@/modules/approvals/components/create-approval-request-dialog";
import { APPROVAL_STATUS_VARIANT } from "@/modules/approvals/constants/status-variant";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

/** How the current user relates to a given row — a request can be both (they created it and
 * later decided one of its steps), so this is a set, not a single value. */
export type ApprovalOwnerRole = "requester" | "decided";

interface ApprovalHistoryTableProps {
  records: ApprovalRequestDto[];
  error?: DataTableErrorState;
  isLoading?: boolean;
  /** userId -> display name, resolved once by the page for every table + the history sheet. */
  userNamesById: Map<string, string>;
  /** requestId -> the current user's role(s) on that request — computed once from the same
   * `relation=All` fetch (requester and/or decided-a-step), then reused as a lookup here instead
   * of re-deriving it per row. */
  rolesById: Map<string, ApprovalOwnerRole[]>;
  onRefresh: () => void;
}

const ROLE_LABEL: Record<ApprovalOwnerRole, string> = {
  requester: "Requester",
  decided: "Decided",
};

/** Read-only self-service view — merges "requests you created" and "requests you decided" into
 * one list, tagged with an owner-role badge per row; unlike `MyApprovalsTable` there are no
 * decide actions here, just history + a "Create request" entry point. */
export function ApprovalHistoryTable({
  records,
  error,
  isLoading,
  userNamesById,
  rolesById,
  onRefresh,
}: ApprovalHistoryTableProps) {
  const [historyRequest, setHistoryRequest] = useState<ApprovalRequestDto | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);

  const actions: DataTableAction[] = [
    {
      key: "create",
      label: "Create request",
      icon: Plus,
      onClick: () => {
        setCreateDialogKey((key) => key + 1);
        setCreateOpen(true);
      },
    },
  ];

  const columns: DataTableColumn<ApprovalRequestDto>[] = [
    {
      id: "requestType",
      header: "Type",
      cell: (request) => request.requestType,
    },
    {
      id: "title",
      header: "Title",
      hideable: false,
      cell: (request) => <span className="font-medium">{request.title}</span>,
    },
    {
      id: "role",
      header: "Role",
      cell: (request) => (
        <div className="flex gap-1">
          {(rolesById.get(request.id) ?? []).map((role) => (
            <Badge key={role} variant="outline">
              {ROLE_LABEL[role]}
            </Badge>
          ))}
        </div>
      ),
    },
    {
      id: "content",
      header: "Content",
      cell: (request) => (
        <p className="max-w-xs whitespace-pre-wrap break-words text-sm text-muted-foreground">
          {request.content}
        </p>
      ),
    },
    {
      id: "requester",
      header: "Requester",
      cell: (request) =>
        request.requesterName ||
        userNamesById.get(request.requesterUserId) ||
        request.requesterUserId,
    },
    {
      id: "level",
      header: "Level",
      cell: (request) => `${request.currentLevel} / ${request.steps.length}`,
    },
    {
      id: "status",
      header: "Status",
      cell: (request) => (
        <Badge variant={APPROVAL_STATUS_VARIANT[request.status]}>{request.status}</Badge>
      ),
    },
    {
      id: "created",
      header: "Requested",
      cell: (request) => <LocalDateTime value={request.created} />,
    },
    {
      id: "history",
      header: "",
      hideable: false,
      cell: (request) => (
        <Button
          size="icon-sm"
          variant="ghost"
          aria-label="View history"
          onClick={() => {
            setHistoryRequest(request);
            setHistoryOpen(true);
          }}
        >
          <History className="size-4" />
        </Button>
      ),
    },
  ];

  return (
    <>
      <DataTable
        columns={columns}
        data={records}
        rowKey={(request) => request.id}
        totalRecords={records.length}
        error={error}
        isLoading={isLoading}
        actions={actions}
        onRefresh={onRefresh}
        emptyState={{
          icon: History,
          title: "No requests yet",
          description: "Requests you create or decide will show up here.",
        }}
      />
      <ApprovalHistorySheet
        open={historyOpen}
        onOpenChange={setHistoryOpen}
        request={historyRequest}
        userNamesById={userNamesById}
      />
      <CreateApprovalRequestDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={onRefresh}
      />
    </>
  );
}
