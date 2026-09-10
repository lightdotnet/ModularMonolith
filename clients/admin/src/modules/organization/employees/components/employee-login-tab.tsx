"use client";

import { useActionState, useEffect, useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Spinner } from "@/components/ui/spinner";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import { useGuardedAction } from "@/hooks/use-guarded-action";
import { getUserDetailAction } from "@/modules/identity/users/api/get-user-detail-action";
import type { UserDto } from "@/modules/identity/users";
import {
  createEmployeeLoginAction,
  type CreateEmployeeLoginFormState,
} from "@/modules/organization/employees/api/create-employee-login-action";
import { linkEmployeeLoginAction } from "@/modules/organization/employees/api/link-employee-login-action";
import { unlinkEmployeeLoginAction } from "@/modules/organization/employees/api/unlink-employee-login-action";
import { UserSelect } from "@/modules/organization/employees/components/user-select";

const initialCreateState: CreateEmployeeLoginFormState = {};

interface EmployeeLoginTabProps {
  employeeId: string;
  userId?: string | null;
  defaultEmail?: string | null;
  defaultPhoneNumber?: string | null;
  onChanged: () => void;
}

export function EmployeeLoginTab({
  employeeId,
  userId,
  defaultEmail,
  defaultPhoneNumber,
  onChanged,
}: EmployeeLoginTabProps) {
  const [unlinking, runUnlink] = useGuardedAction();
  const [linking, runLink] = useGuardedAction();
  const [selectedUserId, setSelectedUserId] = useState("");
  const [selectedUserLabel, setSelectedUserLabel] = useState("");
  const [linkedUser, setLinkedUser] = useState<UserDto | null>(null);
  const [linkedUserLoading, setLinkedUserLoading] = useState(false);
  const [linkedUserFailed, setLinkedUserFailed] = useState(false);

  // Show a basic summary of the already-linked Identity user. The action
  // normalizes a backend 403 (operator without `identity.users.view`) or a
  // missing user into `data: null` — treated as a soft failure that falls back
  // to just the raw user ID. Stale-response guard matches edit-employee-dialog.tsx.
  useEffect(() => {
    if (!userId) return;

    let cancelled = false;

    (async () => {
      setLinkedUserLoading(true);
      setLinkedUserFailed(false);

      const result = await getUserDetailAction(userId);
      if (cancelled) return;

      if (result.data) {
        setLinkedUser(result.data);
      } else {
        setLinkedUserFailed(true);
      }
      setLinkedUserLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [userId]);

  const boundCreateAction = createEmployeeLoginAction.bind(null, employeeId);
  const [createState, createFormAction, createPending] = useActionState(
    boundCreateAction,
    initialCreateState,
  );

  useActionSuccessToast(createState, "Login created and linked.", onChanged);

  function handleUnlink() {
    runUnlink(() => unlinkEmployeeLoginAction(employeeId), "Login unlinked.", onChanged);
  }

  function handleLink() {
    if (!selectedUserId) return;
    runLink(() => linkEmployeeLoginAction(employeeId, selectedUserId), "Login linked.", onChanged);
  }

  if (userId) {
    return (
      <div className="flex flex-col gap-3">
        {linkedUserLoading ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Spinner />
            Loading linked account...
          </p>
        ) : linkedUser ? (
          <div className="flex flex-col gap-2 text-sm">
            <p>
              <span className="font-medium">
                {`${linkedUser.firstName ?? ""} ${linkedUser.lastName ?? ""}`.trim()}
              </span>{" "}
              <span className="text-muted-foreground">@{linkedUser.userName}</span>
            </p>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs text-muted-foreground">Email</span>
              <span>{linkedUser.email ?? ""}</span>
            </div>
            <div className="flex flex-col gap-0.5">
              <span className="text-xs text-muted-foreground">Phone number</span>
              <span>{linkedUser.phoneNumber ?? ""}</span>
            </div>
            {linkedUser.status && (
              <div className="flex flex-col gap-0.5">
                <span className="text-xs text-muted-foreground">Status</span>
                <span>{linkedUser.status}</span>
              </div>
            )}
            {linkedUser.isDeleted && (
              <p className="text-sm text-destructive">This account has been deleted.</p>
            )}
          </div>
        ) : (
          <div className="flex flex-col gap-1">
            <p className="text-sm">
              This employee has a login account linked (user ID <code>{userId}</code>).
            </p>
            {linkedUserFailed && (
              <p className="text-xs text-muted-foreground">
                Unable to load linked account details.
              </p>
            )}
          </div>
        )}
        <div>
          <Button variant="outline" loading={unlinking} onClick={handleUnlink}>
            Unlink login
          </Button>
        </div>
        <p className="text-xs text-muted-foreground">
          Unlinking only removes the association — the Identity user account itself is not
          deleted.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <p className="text-sm font-medium">Create a new login</p>
        <form action={createFormAction} className="flex flex-col gap-3" autoComplete="off">
          {createState.error && (
            <Alert variant="destructive">
              <AlertDescription>{createState.error}</AlertDescription>
            </Alert>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="userName">Username</Label>
              <Input id="userName" name="userName" autoComplete="off" required />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="new-password"
                required
              />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="loginEmail">Email</Label>
              <Input id="loginEmail" name="email" type="email" defaultValue={defaultEmail ?? ""} />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="loginPhone">Phone number</Label>
              <Input id="loginPhone" name="phoneNumber" defaultValue={defaultPhoneNumber ?? ""} />
            </div>
          </div>
          <div>
            <Button type="submit" loading={createPending}>
              Create login
            </Button>
          </div>
        </form>
      </div>

      <Separator />

      <div className="flex flex-col gap-3">
        <p className="text-sm font-medium">Link an existing user</p>
        <UserSelect
          value={selectedUserId}
          onValueChange={(user) => {
            setSelectedUserId(user.id);
            setSelectedUserLabel(user.userName);
          }}
          placeholder="Search for a user..."
        />
        <div>
          <Button
            type="button"
            variant="outline"
            loading={linking}
            disabled={!selectedUserId}
            onClick={handleLink}
          >
            Link {selectedUserLabel ? `"${selectedUserLabel}"` : "user"}
          </Button>
        </div>
      </div>
    </div>
  );
}
