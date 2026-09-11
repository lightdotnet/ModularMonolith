"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EditLeaveRequestDialog } from "@/modules/leave-requests/components/edit-leave-request-dialog";
import { DeleteLeaveRequestDialog } from "@/modules/leave-requests/components/delete-leave-request-dialog";
import type { LeaveRequestDto } from "@/modules/leave-requests/types/leave-request";

interface LeaveRequestDetailActionsProps {
  leaveRequest: LeaveRequestDto;
  canEdit: boolean;
  canDelete: boolean;
  canManage: boolean;
}

export function LeaveRequestDetailActions({
  leaveRequest,
  canEdit,
  canDelete,
  canManage,
}: LeaveRequestDetailActionsProps) {
  const router = useRouter();
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  if (!canEdit && !canDelete) return null;

  return (
    <div className="flex gap-2">
      {canEdit && (
        <Button size="sm" variant="outline" onClick={() => setEditOpen(true)}>
          <Pencil className="size-4" />
          Edit
        </Button>
      )}
      {canDelete && (
        <Button size="sm" variant="destructive" onClick={() => setDeleteOpen(true)}>
          <Trash2 className="size-4" />
          Delete
        </Button>
      )}

      <EditLeaveRequestDialog
        open={editOpen}
        onOpenChange={setEditOpen}
        leaveRequest={leaveRequest}
        onUpdated={() => router.refresh()}
        canManage={canManage}
      />
      <DeleteLeaveRequestDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        leaveRequest={leaveRequest}
        onDeleted={() => router.push("/leave-requests")}
      />
    </div>
  );
}
