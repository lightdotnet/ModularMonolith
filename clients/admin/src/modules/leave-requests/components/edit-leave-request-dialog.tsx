"use client";

import { useActionState, useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  updateLeaveRequestAction,
  type UpdateLeaveRequestFormState,
} from "@/modules/leave-requests/api/update-leave-request-action";
import { getApproverCandidatesAction } from "@/modules/leave-requests/api/get-approver-candidates-action";
import {
  LeaveType,
  type ApproverCandidateDto,
  type LeaveRequestDto,
} from "@/modules/leave-requests/types/leave-request";

const updateInitialState: UpdateLeaveRequestFormState = {};

const LEAVE_TYPE_OPTIONS = Object.values(LeaveType).map((value) => ({
  value,
  label: value,
}));

interface EditLeaveRequestDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  leaveRequest: LeaveRequestDto | null;
  onUpdated: () => void;
  /** A manage-scoped edit is metadata-only and never touches Approval, so no approver field is
   * shown (or fetched) in that case — only self-service edits (resubmissions) need one. */
  canManage: boolean;
}

export function EditLeaveRequestDialog({
  open,
  onOpenChange,
  leaveRequest,
  onUpdated,
  canManage,
}: EditLeaveRequestDialogProps) {
  const [state, formAction, pending] = useActionState(updateLeaveRequestAction, updateInitialState);
  const [loadingApprovers, setLoadingApprovers] = useState(!canManage);
  const [approversError, setApproversError] = useState("");
  const [candidates, setCandidates] = useState<ApproverCandidateDto[]>([]);

  useEffect(() => {
    if (!open || canManage) return;

    let cancelled = false;

    (async () => {
      setLoadingApprovers(true);
      setApproversError("");

      const result = await getApproverCandidatesAction();
      if (cancelled) return;

      if (!result.data) {
        setApproversError(result.error || "Unable to load approvers.");
        setLoadingApprovers(false);
        return;
      }

      setCandidates(result.data);
      setLoadingApprovers(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [open, canManage]);

  const approverOptions = candidates.map((candidate) => ({
    value: candidate.employeeId,
    label: candidate.name || candidate.employeeId,
  }));

  useActionSuccessToast(state, "Leave request updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!leaveRequest) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="id" value={leaveRequest.id} />

          <DialogHeader>
            <DialogTitle>Edit leave request</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <NativeSelect
            id="leaveType"
            name="leaveType"
            label="Leave type"
            options={LEAVE_TYPE_OPTIONS}
            defaultValue={leaveRequest.leaveType}
            required
          />

          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="startDate">Start date</Label>
              <Input
                id="startDate"
                name="startDate"
                type="date"
                defaultValue={leaveRequest.startDate.slice(0, 10)}
                required
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="endDate">End date</Label>
              <Input
                id="endDate"
                name="endDate"
                type="date"
                defaultValue={leaveRequest.endDate.slice(0, 10)}
                required
              />
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="reason">Reason</Label>
            <Textarea id="reason" name="reason" defaultValue={leaveRequest.reason ?? ""} />
          </div>

          {!canManage &&
            (approversError ? (
              <Alert variant="destructive">
                <AlertDescription>{approversError}</AlertDescription>
              </Alert>
            ) : !loadingApprovers && approverOptions.length === 0 ? (
              <Alert variant="destructive">
                <AlertDescription>
                  No approver is available for your department — contact an administrator.
                </AlertDescription>
              </Alert>
            ) : (
              <NativeSelect
                id="approverEmployeeId"
                name="approverEmployeeId"
                label="Approver"
                placeholder="Select an approver"
                options={approverOptions}
                loading={loadingApprovers}
                required
              />
            ))}

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              loading={pending || (!canManage && loadingApprovers)}
              disabled={
                !canManage && !loadingApprovers && (approverOptions.length === 0 || !!approversError)
              }
            >
              Save
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
