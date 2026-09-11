"use client";

import { useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Spinner } from "@/components/ui/spinner";
import { getOrgUnitManagersAction } from "@/modules/organization/departments/api/get-org-unit-managers-action";
import type { OrgUnitTreeNodeDto } from "@/modules/organization/departments/types/org-unit";
import type { EmployeeDto } from "@/modules/organization/employees";

interface ViewOrgUnitManagersDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  node: OrgUnitTreeNodeDto | null;
}

export function ViewOrgUnitManagersDialog({ open, onOpenChange, node }: ViewOrgUnitManagersDialogProps) {
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [managers, setManagers] = useState<EmployeeDto[]>([]);

  useEffect(() => {
    if (!node) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const result = await getOrgUnitManagersAction(node.id);
      if (cancelled) return;

      if (!result.data) {
        setLoadError(result.error || "Unable to load managers.");
        setLoading(false);
        return;
      }

      setManagers(result.data);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the dialog remounts fresh (via `key`) on every open
  }, []);

  if (!node) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Managers of &quot;{node.name}&quot;</DialogTitle>
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
        ) : managers.length === 0 ? (
          <p className="text-sm text-muted-foreground">No manager assigned to this department/team.</p>
        ) : (
          <div className="flex flex-col gap-2">
            {managers.map((manager) => (
              <div key={manager.id} className="flex flex-col rounded-md border border-border p-2.5">
                <span className="font-medium">
                  {manager.firstName} {manager.lastName}
                </span>
                <span className="text-xs text-muted-foreground">
                  {manager.employeeCode}
                  {manager.email ? ` · ${manager.email}` : ""}
                </span>
              </div>
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
