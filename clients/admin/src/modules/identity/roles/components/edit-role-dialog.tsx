"use client";

import { useActionState, useEffect, useState } from "react";
import { X } from "lucide-react";
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
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { getRoleDetailAction } from "@/modules/identity/roles/api/get-role-detail-action";
import { getPermissionsAction } from "@/modules/identity/roles/api/get-permissions-action";
import {
  updateRoleAction,
  type UpdateRoleFormState,
} from "@/modules/identity/roles/api/update-role-action";
import type { PermissionDefinition } from "@/modules/identity/roles/types/permission-definition";
import type { RoleDto } from "@/modules/identity/roles/types/role";
import type { ClaimDto } from "@/types/claim";

const updateInitialState: UpdateRoleFormState = {};
const PERMISSION_CLAIM_TYPE = "permission";

function groupPermissions(
  permissions: PermissionDefinition[],
): [string, PermissionDefinition[]][] {
  const groups = new Map<string, PermissionDefinition[]>();

  for (const permission of permissions) {
    const group = permission.parent || "other";
    const list = groups.get(group) ?? [];
    list.push(permission);
    groups.set(group, list);
  }

  return Array.from(groups.entries());
}

interface EditRoleDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  role: RoleDto | null;
  onUpdated: () => void;
}

export function EditRoleDialog({
  open,
  onOpenChange,
  role,
  onUpdated,
}: EditRoleDialogProps) {
  const [state, formAction, pending] = useActionState(
    updateRoleAction,
    updateInitialState,
  );

  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");
  const [detail, setDetail] = useState<RoleDto | null>(null);
  const [permissions, setPermissions] = useState<PermissionDefinition[]>([]);
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
  const [otherClaims, setOtherClaims] = useState<ClaimDto[]>([]);
  const [newClaimType, setNewClaimType] = useState("");
  const [newClaimValue, setNewClaimValue] = useState("");
  const [claimError, setClaimError] = useState("");

  // The roles list only carries name/description — claims aren't populated
  // there, so load the full record (plus the assignable permission list)
  // once the dialog opens.
  useEffect(() => {
    if (!role) return;

    let cancelled = false;

    (async () => {
      setLoading(true);
      setLoadError("");

      const [detailResult, permissionsResult] = await Promise.all([
        getRoleDetailAction(role.id),
        getPermissionsAction(),
      ]);
      if (cancelled) return;

      if (!detailResult.data) {
        setLoadError(detailResult.error || "Unable to load role details.");
        setLoading(false);
        return;
      }

      // A "permission"-type claim only renders as a checkbox if its value is
      // among the fetched permission definitions — anything else (including
      // every permission claim when the fetch itself fails) falls through to
      // the Other claims section instead of being silently dropped on save.
      const permissionDefinitions = permissionsResult.data ?? [];
      const knownPermissionNames = new Set(
        permissionDefinitions.map((permission) => permission.name),
      );
      const isKnownPermissionClaim = (claim: ClaimDto) =>
        claim.type === PERMISSION_CLAIM_TYPE &&
        knownPermissionNames.has(claim.value);

      setDetail(detailResult.data);
      setSelectedPermissions(
        detailResult.data.claims
          .filter(isKnownPermissionClaim)
          .map((claim) => claim.value),
      );
      setOtherClaims(
        detailResult.data.claims.filter(
          (claim) => !isKnownPermissionClaim(claim),
        ),
      );
      setPermissions(permissionDefinitions);
      setLoading(false);
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- fetch once per mount; the dialog remounts fresh (via `key`) on every open
  }, []);

  useActionSuccessToast(state, "Role updated.", () => {
    onUpdated();
    onOpenChange(false);
  });

  function togglePermission(name: string, checked: boolean) {
    setSelectedPermissions((previous) =>
      checked
        ? [...previous, name]
        : previous.filter((value) => value !== name),
    );
  }

  function addOtherClaim() {
    const type = newClaimType.trim();
    const value = newClaimValue.trim();

    if (!type || !value) {
      setClaimError("Both type and value are required.");
      return;
    }
    if (
      otherClaims.some((claim) => claim.type === type && claim.value === value)
    ) {
      setClaimError("This claim already exists.");
      return;
    }

    setOtherClaims((previous) => [...previous, { type, value }]);
    setNewClaimType("");
    setNewClaimValue("");
    setClaimError("");
  }

  function removeOtherClaim(index: number) {
    setOtherClaims((previous) => previous.filter((_, i) => i !== index));
  }

  if (!role) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-lg"
        onPointerDownOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Edit role</DialogTitle>
        </DialogHeader>

        {loading ? (
          <div className="flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground">
            <Spinner />
            Loading role details...
          </div>
        ) : loadError ? (
          <Alert variant="destructive">
            <AlertDescription>{loadError}</AlertDescription>
          </Alert>
        ) : (
          detail && (
            <form action={formAction} className="flex flex-col gap-4">
              <input type="hidden" name="id" value={detail.id} />
              <input
                type="hidden"
                name="otherClaims"
                value={JSON.stringify(otherClaims)}
              />

              {state.error && (
                <Alert variant="destructive">
                  <AlertDescription>{state.error}</AlertDescription>
                </Alert>
              )}

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  name="name"
                  defaultValue={detail.name}
                  required
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="description">Description</Label>
                <Input
                  id="description"
                  name="description"
                  defaultValue={detail.description ?? ""}
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <Label>Permissions</Label>
                <div className="flex max-h-56 flex-col gap-3 overflow-y-auto rounded-md border border-border p-3">
                  {permissions.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      No permissions available.
                    </p>
                  ) : (
                    groupPermissions(permissions).map(([group, items]) => (
                      <div key={group} className="flex flex-col gap-1.5">
                        <p className="text-xs font-medium uppercase text-muted-foreground">
                          {group}
                        </p>
                        {items.map((permission) => (
                          <label
                            key={permission.name}
                            className="flex items-center gap-2 text-sm"
                          >
                            <Checkbox
                              name="permissions"
                              value={permission.name}
                              checked={selectedPermissions.includes(
                                permission.name,
                              )}
                              onCheckedChange={(checked) =>
                                togglePermission(
                                  permission.name,
                                  checked === true,
                                )
                              }
                            />
                            {permission.displayName}
                          </label>
                        ))}
                      </div>
                    ))
                  )}
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label>Other claims</Label>
                <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                  {otherClaims.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      No custom claims.
                    </p>
                  ) : (
                    <div className="flex flex-col gap-1.5">
                      {otherClaims.map((claim, index) => (
                        <div
                          key={`${claim.type}:${claim.value}:${index}`}
                          className="flex items-center justify-between gap-2 text-sm"
                        >
                          <span className="truncate">
                            {claim.type}: {claim.value}
                          </span>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon-xs"
                            aria-label={`Remove claim ${claim.type}: ${claim.value}`}
                            onClick={() => removeOtherClaim(index)}
                          >
                            <X />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="flex items-end gap-2 border-t border-border pt-2">
                    <div className="flex flex-1 flex-col gap-1.5">
                      <Label htmlFor="newClaimType">Type</Label>
                      <Input
                        id="newClaimType"
                        value={newClaimType}
                        onChange={(event) =>
                          setNewClaimType(event.target.value)
                        }
                      />
                    </div>
                    <div className="flex flex-1 flex-col gap-1.5">
                      <Label htmlFor="newClaimValue">Value</Label>
                      <Input
                        id="newClaimValue"
                        value={newClaimValue}
                        onChange={(event) =>
                          setNewClaimValue(event.target.value)
                        }
                      />
                    </div>
                    <Button type="button" variant="outline" onClick={addOtherClaim}>
                      Add
                    </Button>
                  </div>
                  {claimError && (
                    <p className="text-sm text-destructive">{claimError}</p>
                  )}
                </div>
              </div>

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
          )
        )}
      </DialogContent>
    </Dialog>
  );
}
