"use client";

import { useActionState, useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
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
import { Spinner } from "@/components/ui/spinner";
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { getDocumentTypeDetailAction } from "@/modules/approvals/api/get-document-type-detail-action";
import {
  updateDocumentTypeAction,
  type UpdateDocumentTypeFormState,
} from "@/modules/approvals/api/update-document-type-action";
import type { ApprovalDocumentTypeDto } from "@/modules/approvals/types/document-type";

const updateInitialState: UpdateDocumentTypeFormState = {};

interface EditDocumentTypeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  documentType: ApprovalDocumentTypeDto | null;
  onUpdated: () => void;
}

export function EditDocumentTypeDialog({
  open,
  onOpenChange,
  documentType,
  onUpdated,
}: EditDocumentTypeDialogProps) {
  const [state, formAction, pending] = useActionState(updateDocumentTypeAction, updateInitialState);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [detail, setDetail] = useState<ApprovalDocumentTypeDto | null>(null);
  const [isActive, setIsActive] = useState(true);

  // The table row only carries list-level fields; load the full record (incl.
  // `created`, needed by the PUT body) once the dialog opens.
  useEffect(() => {
    if (!documentType) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getDocumentTypeDetailAction(documentType.id);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load document type.");
        setLoading(false);
        return;
      }

      setDetail(result.data);
      setIsActive(result.data.isActive);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the dialog remounts fresh (via `key`) on every open
  }, []);

  useActionSuccessToast(state, "Document type updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  if (!documentType) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Edit document type</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Spinner />
            Loading document type...
          </div>
        ) : loadError || !detail ? (
          <Alert variant="destructive">
            <AlertDescription>{loadError || "Unable to load document type."}</AlertDescription>
          </Alert>
        ) : (
          <form action={formAction} className="flex flex-col gap-4">
            <input type="hidden" name="id" value={detail.id} />
            <input type="hidden" name="created" value={detail.created} />
            <input type="hidden" name="isActive" value={isActive ? "true" : "false"} />

            {state.error && (
              <Alert variant="destructive">
                <AlertDescription>{state.error}</AlertDescription>
              </Alert>
            )}

            <div className="grid grid-cols-2 gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="name">Name</Label>
                <Input id="name" name="name" defaultValue={detail.name} required />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="code">Code</Label>
                <Input id="code" name="code" defaultValue={detail.code} required />
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="description">Description</Label>
              <Textarea id="description" name="description" defaultValue={detail.description ?? ""} />
            </div>
            <label className="flex items-center gap-2 text-sm">
              <Checkbox
                checked={isActive}
                onCheckedChange={(checked) => setIsActive(checked === true)}
              />
              Active
            </label>

            <DialogFooter>
              <DialogClose asChild>
                <Button type="button" variant="outline">
                  Cancel
                </Button>
              </DialogClose>
              <Button type="submit" loading={pending}>
                Save
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
