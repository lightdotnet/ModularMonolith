"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Check, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DecideApprovalDialog } from "@/modules/approvals/components/decide-approval-dialog";
import {
  ApprovalStatus,
  ApprovalStepStatus,
  type ApprovalRequestDto,
} from "@/modules/approvals/types/approval";

interface ApprovalDecisionActionsProps {
  request: ApprovalRequestDto;
  /** The viewing user's id — decides whether the Approve/Reject controls show. */
  currentUserId: string;
}

/** Approve/Reject controls for the request detail page, shown only when the current user is the
 * assigned approver for the level the request is currently waiting on. */
export function ApprovalDecisionActions({ request, currentUserId }: ApprovalDecisionActionsProps) {
  const router = useRouter();
  const [approved, setApproved] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);

  const currentStep = request.steps.find((step) => step.level === request.currentLevel);
  const canDecide =
    request.status === ApprovalStatus.Pending &&
    currentStep?.approverUserId === currentUserId &&
    currentStep?.status === ApprovalStepStatus.Pending;

  if (!canDecide) {
    return null;
  }

  function openDecision(willApprove: boolean) {
    setApproved(willApprove);
    setDialogOpen(true);
  }

  return (
    <>
      <div className="flex flex-wrap gap-2">
        <Button variant="outline" onClick={() => openDecision(false)}>
          <X className="size-4" />
          Reject
        </Button>
        <Button onClick={() => openDecision(true)}>
          <Check className="size-4" />
          Approve
        </Button>
      </div>
      <DecideApprovalDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        request={request}
        approved={approved}
        onDecided={() => router.refresh()}
      />
    </>
  );
}
