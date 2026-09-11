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
  createLeaveRequestAction,
  type CreateLeaveRequestFormState,
} from "@/modules/leave-requests/api/create-leave-request-action";
import { getApproverCandidatesAction } from "@/modules/leave-requests/api/get-approver-candidates-action";
import { LeaveType, type ApproverCandidateDto } from "@/modules/leave-requests/types/leave-request";

const initialState: CreateLeaveRequestFormState = {};

const LEAVE_TYPE_OPTIONS = Object.values(LeaveType).map((value) => ({
  value,
  label: value,
}));

interface FormValues {
  leaveType: LeaveType | "";
  startDate: string;
  endDate: string;
  reason: string;
  approverEmployeeId: string;
}

const initialValues: FormValues = {
  leaveType: "",
  startDate: "",
  endDate: "",
  reason: "",
  approverEmployeeId: "",
};

interface CreateLeaveRequestDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}

export function CreateLeaveRequestDialog({
  open,
  onOpenChange,
  onCreated,
}: CreateLeaveRequestDialogProps) {
  const [state, formAction, pending] = useActionState(createLeaveRequestAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);
  const [loadingApprovers, setLoadingApprovers] = useState(true);
  const [approversError, setApproversError] = useState("");
  const [candidates, setCandidates] = useState<ApproverCandidateDto[]>([]);

  // Fetches once per mount — this dialog remounts fresh (via `key`) on every open.
  useEffect(() => {
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
  }, []);

  const approverOptions = candidates.map((candidate) => ({
    value: candidate.employeeId,
    label: candidate.name || candidate.employeeId,
  }));

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function reset() {
    setValues(initialValues);
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) reset();
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Leave request submitted.", () => {
    onCreated();
    onOpenChange(false);
    reset();
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>New leave request</DialogTitle>
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
            placeholder="Select a leave type"
            options={LEAVE_TYPE_OPTIONS}
            value={values.leaveType}
            onChange={(value) => setValues((previous) => ({ ...previous, leaveType: value }))}
            required
          />

          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="startDate">Start date</Label>
              <Input
                id="startDate"
                name="startDate"
                type="date"
                value={values.startDate}
                onChange={setField("startDate")}
                required
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="endDate">End date</Label>
              <Input
                id="endDate"
                name="endDate"
                type="date"
                value={values.endDate}
                onChange={setField("endDate")}
                required
              />
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="reason">Reason</Label>
            <Textarea id="reason" name="reason" value={values.reason} onChange={setField("reason")} />
          </div>

          {approversError ? (
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
              value={values.approverEmployeeId}
              onChange={(value) =>
                setValues((previous) => ({ ...previous, approverEmployeeId: value }))
              }
              loading={loadingApprovers}
              required
            />
          )}

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              loading={pending || loadingApprovers}
              disabled={!loadingApprovers && (approverOptions.length === 0 || !!approversError)}
            >
              Submit
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
