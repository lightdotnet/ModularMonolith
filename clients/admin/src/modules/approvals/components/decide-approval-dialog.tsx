"use client";

import { useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { decideApprovalAction } from "@/modules/approvals/api/decide-approval-action";
import type { ApprovalRequestDto } from "@/modules/approvals/types/approval";

interface DecideApprovalDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  request: ApprovalRequestDto | null;
  approved: boolean;
  onDecided: () => void;
}

export function DecideApprovalDialog({
  open,
  onOpenChange,
  request,
  approved,
  onDecided,
}: DecideApprovalDialogProps) {
  const [pending, run] = useGuardedAction();
  const [comment, setComment] = useState("");
  const [error, setError] = useState<string | undefined>();

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setComment("");
      setError(undefined);
    }
    onOpenChange(nextOpen);
  }

  function handleConfirm() {
    if (!request) return;

    if (!approved && !comment.trim()) {
      setError("A reason is required when rejecting a request.");
      return;
    }

    setError(undefined);

    run(
      () => decideApprovalAction(request.id, approved, comment),
      approved ? "Request approved." : "Request rejected.",
      () => {
        handleOpenChange(false);
        onDecided();
      },
    );
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>{approved ? "Approve request" : "Reject request"}</DialogTitle>
          <DialogDescription>
            Level {request?.currentLevel} of your approval chain.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-1.5">
          <p className="font-medium">{request?.title}</p>
          {request?.content && (
            <p className="whitespace-pre-wrap rounded-md border bg-muted/50 p-3 text-sm">{request.content}</p>
          )}
        </div>

        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="comment">{approved ? "Note (optional)" : "Reason"}</Label>
          <Textarea
            id="comment"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            rows={3}
            required={!approved}
          />
        </div>

        <DialogFooter>
          <Button disabled={pending} onClick={() => handleOpenChange(false)} type="button" variant="outline">
            Cancel
          </Button>
          <Button
            disabled={pending}
            loading={pending}
            onClick={handleConfirm}
            type="button"
            variant={approved ? "default" : "destructive"}
          >
            {approved ? "Approve" : "Reject"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
