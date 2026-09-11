"use client";

import { useState } from "react";
import { Check, ClipboardCheck, History, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DataTable,
  type DataTableColumn,
  type DataTableErrorState,
} from "@/components/shared/data-table";
import { LocalDateTime } from "@/components/shared/local-date-time";
import { ApprovalHistorySheet } from "@/modules/approvals/components/approval-history-sheet";
import { DecideApprovalDialog } from "@/modules/approvals/components/decide-approval-dialog";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

interface MyApprovalsTableProps {
  records: ApprovalRequestDto[];
  error?: DataTableErrorState;
  isLoading?: boolean;
  /** userId -> display name, resolved once by the page for every table + the history sheet. */
  userNamesById: Map<string, string>;
  onRefresh: () => void;
}

export function MyApprovalsTable({
  records,
  error,
  isLoading,
  userNamesById,
  onRefresh,
}: MyApprovalsTableProps) {
  const [selected, setSelected] = useState<ApprovalRequestDto | null>(null);
  const [approved, setApproved] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [historyRequest, setHistoryRequest] = useState<ApprovalRequestDto | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);

  function openDecision(request: ApprovalRequestDto, willApprove: boolean) {
    setSelected(request);
    setApproved(willApprove);
    setDialogOpen(true);
  }

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
      cell: (request) => `#${request.currentLevel}`,
    },
    {
      id: "created",
      header: "Requested",
      cell: (request) => <LocalDateTime value={request.created} />,
    },
    {
      id: "actions",
      header: "",
      hideable: false,
      cell: (request) => (
        <div className="flex justify-end gap-2">
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
          <Button size="sm" variant="outline" onClick={() => openDecision(request, false)}>
            <X className="size-4" />
            Reject
          </Button>
          <Button size="sm" onClick={() => openDecision(request, true)}>
            <Check className="size-4" />
            Approve
          </Button>
        </div>
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
        onRefresh={onRefresh}
        emptyState={{
          icon: ClipboardCheck,
          title: "Nothing waiting on you",
          description: "Requests routed to you for approval will show up here.",
        }}
      />
      <DecideApprovalDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        request={selected}
        approved={approved}
        onDecided={onRefresh}
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
