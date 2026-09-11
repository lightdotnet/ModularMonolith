"use client";

import { useState } from "react";
import { History, ListChecks, Plus } from "lucide-react";
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
import { createTestApprovalRequestAction } from "@/modules/approvals/api/create-test-approval-request-action";
import { APPROVAL_STATUS_VARIANT } from "@/modules/approvals/constants/status-variant";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

interface AllApprovalsTableProps {
  records: ApprovalRequestDto[];
  error?: DataTableErrorState;
  isLoading?: boolean;
  canCreate?: boolean;
  /** userId -> display name, resolved once by the page for every table + the history sheet. */
  userNamesById: Map<string, string>;
  onRefresh: () => void;
}

/** Read-only, most-recent-50 view — lets you watch a chain's status/level advance across decisions without a full search/pagination UI (this is an admin verification view, not a primary workflow). */
export function AllApprovalsTable({
  records,
  error,
  isLoading,
  canCreate,
  userNamesById,
  onRefresh,
}: AllApprovalsTableProps) {
  const [createOpen, setCreateOpen] = useState(false);
  const [createDialogKey, setCreateDialogKey] = useState(0);
  const [historyRequest, setHistoryRequest] = useState<ApprovalRequestDto | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);

  const actions: DataTableAction[] | undefined = canCreate
    ? [
        {
          key: "create",
          label: "Create test request",
          icon: Plus,
          onClick: () => {
            setCreateDialogKey((key) => key + 1);
            setCreateOpen(true);
          },
        },
      ]
    : undefined;

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
          icon: ListChecks,
          title: "No approval requests yet",
          description: "Create a test request to see it here.",
        }}
      />
      <CreateApprovalRequestDialog
        key={`create-${createDialogKey}`}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={onRefresh}
        dialogTitle="Create test approval request"
        action={createTestApprovalRequestAction}
      />
      <ApprovalHistorySheet
        open={historyOpen}
        onOpenChange={setHistoryOpen}
        request={historyRequest}
        userNamesById={userNamesById}
      />
    </>
  );
}
