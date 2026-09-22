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
import { getLocationTreeAction } from "@/modules/location/api/get-location-tree-action";
import { moveLocationAction } from "@/modules/location/api/move-location-action";
import { flattenLocationTree, type LocationTreeNodeDto } from "@/modules/location/types/location";

interface MoveLocationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  node: LocationTreeNodeDto | null;
  onMoved: () => void;
}

const NONE_VALUE = "__none__";

export function MoveLocationDialog({
  open,
  onOpenChange,
  node,
  onMoved,
}: MoveLocationDialogProps) {
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [options, setOptions] = useState<LocationTreeNodeDto[]>([]);
  const [newParentId, setNewParentId] = useState(NONE_VALUE);
  const [moving, run] = useGuardedAction();

  useEffect(() => {
    if (!node) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getLocationTreeAction();
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load locations.");
        setLoading(false);
        return;
      }

      const flat = flattenLocationTree(result.data).filter((item) => item.id !== node.id);
      setOptions(flat);
      setNewParentId(node.parentLocationId || NONE_VALUE);
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
      () => moveLocationAction(node.id, newParentId === NONE_VALUE ? undefined : newParentId),
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
                { value: NONE_VALUE, label: "No parent (top-level location)" },
                ...options.map((option) => ({
                  value: option.id,
                  label: option.name,
                })),
              ]}
            />
            <p className="text-xs text-muted-foreground">
              Moving under one of its own descendants is rejected by the server.
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
