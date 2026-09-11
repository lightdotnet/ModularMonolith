"use client";

import { useEffect, useState } from "react";
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
import { NativeSelect } from "@/components/ui/native-select";
import { Spinner } from "@/components/ui/spinner";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { getOrgUnitTreeAction } from "@/modules/organization/departments/api/get-org-unit-tree-action";
import { moveOrgUnitAction } from "@/modules/organization/departments/api/move-org-unit-action";
import { flattenOrgUnitTree, type OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";

interface MoveOrgUnitDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  companyId: string;
  node: OrgUnitTreeNodeDto | null;
  onMoved: () => void;
}

const NONE_VALUE = "__none__";

export function MoveOrgUnitDialog({
  open,
  onOpenChange,
  companyId,
  node,
  onMoved,
}: MoveOrgUnitDialogProps) {
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [options, setOptions] = useState<OrgUnitTreeNodeDto[]>([]);
  const [newParentId, setNewParentId] = useState(NONE_VALUE);
  const [moving, run] = useGuardedAction();

  useEffect(() => {
    if (!node) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getOrgUnitTreeAction(companyId);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load departments/teams.");
        setLoading(false);
        return;
      }

      const flat = flattenOrgUnitTree(result.data).filter((item) => item.id !== node.id);
      setOptions(flat);
      setNewParentId(node.parentId || NONE_VALUE);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the dialog remounts fresh (via `key`) on every open
  }, []);

  function handleConfirm() {
    if (!node) return;

    run(
      () => moveOrgUnitAction(node.id, newParentId === NONE_VALUE ? undefined : newParentId),
      `"${node.name}" moved.`,
      () => {
        onOpenChange(false);
        onMoved();
      },
    );
  }

  if (!node) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Move &quot;{node.name}&quot;</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Spinner />
            Loading...
          </div>
        ) : loadError ? (
          <Alert variant="destructive">
            <AlertDescription>{loadError}</AlertDescription>
          </Alert>
        ) : (
          <div className="flex flex-col gap-1.5">
            <NativeSelect
              label="New parent"
              value={newParentId}
              onChange={setNewParentId}
              options={[
                { value: NONE_VALUE, label: "No parent (top-level department)" },
                ...options.map((option) => ({
                  value: option.id,
                  label: `${option.name} (${option.type})`,
                })),
              ]}
            />
            <p className="text-xs text-muted-foreground">
              Moving under one of its own descendants, or to a different company, is rejected by
              the server.
            </p>
          </div>
        )}

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button type="button" loading={moving} disabled={loading || !!loadError} onClick={handleConfirm}>
            Move
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
