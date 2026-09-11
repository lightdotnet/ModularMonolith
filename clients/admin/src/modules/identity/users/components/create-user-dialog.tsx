"use client";

import { useActionState, useRef, useState } from "react";
import { SearchIcon } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Combobox } from "@/components/ui/combobox";
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
import {
  createUserAction,
  type CreateUserFormState,
} from "@/modules/identity/users/api/create-user-action";
import { getDomainUserAction } from "@/modules/identity/users/api/get-domain-user-action";
import { AUTH_PROVIDER_SELECT_OPTIONS } from "@/modules/identity/users/constants/auth-provider";

// Domain lookup on blur only fires once the username is at least this long.
const MIN_USERNAME_LOOKUP_LENGTH = 3;

const initialState: CreateUserFormState = {};

interface FormValues {
  userName: string;
  password: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  authProvider: string;
}

const initialValues: FormValues = {
  userName: "",
  password: "",
  firstName: "",
  lastName: "",
  email: "",
  phoneNumber: "",
  authProvider: "",
};

interface CreateUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: () => void;
}

export function CreateUserDialog({ open, onOpenChange, onCreated }: CreateUserDialogProps) {
  const [state, formAction, pending] = useActionState(createUserAction, initialState);
  // Fields are controlled so a failed submit (React resets uncontrolled form
  // fields once the action settles) doesn't wipe what the admin already typed.
  const [values, setValues] = useState<FormValues>(initialValues);
  const [isLookingUp, setIsLookingUp] = useState(false);
  const [lookupFound, setLookupFound] = useState(false);
  // Tracks the last username a domain lookup ran for, so blurring the field
  // without having changed it doesn't refire the search.
  const lastLookedUpRef = useRef("");

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement>) => {
      if (field === "userName") setLookupFound(false);
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
    };
  }

  // Every close path (Cancel, the header's X button, Escape, a successful
  // create) funnels through here — reset the form so the next open starts clean.
  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setValues(initialValues);
      setLookupFound(false);
      lastLookedUpRef.current = "";
    }
    onOpenChange(nextOpen);
  }

  async function lookupDomainUser(userName: string) {
    setIsLookingUp(true);
    setLookupFound(false);
    lastLookedUpRef.current = userName;

    const result = await getDomainUserAction(userName);

    setIsLookingUp(false);

    if (!result.data) {
      // Not found just means this will be a local account — no error to show,
      // just drop any stale domain data a previous lookup may have filled in.
      setValues((previous) => ({
        ...previous,
        firstName: "",
        lastName: "",
        email: "",
        phoneNumber: "",
        authProvider: "",
      }));
      return;
    }

    setLookupFound(true);

    setValues((previous) => ({
      ...previous,
      firstName: result.data!.firstName ?? "",
      lastName: result.data!.lastName ?? "",
      email: result.data!.email ?? "",
      phoneNumber: result.data!.phoneNumber ?? "",
      authProvider: "AD",
    }));
  }

  function handleLookupClick() {
    const userName = values.userName.trim();
    if (!userName) return;
    void lookupDomainUser(userName);
  }

  // Fires once the admin finishes editing the username (not per keystroke).
  function handleUsernameBlur() {
    const userName = values.userName.trim();
    if (userName.length < MIN_USERNAME_LOOKUP_LENGTH) return;
    if (userName === lastLookedUpRef.current) return;
    void lookupDomainUser(userName);
  }

  useActionSuccessToast(state, "User created.", () => {
    onCreated();
    onOpenChange(false);
    setValues(initialValues);
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4" autoComplete="off">
          <DialogHeader>
            <DialogTitle>Create user</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="userName">Username</Label>
            <div className="relative">
              <Input
                id="userName"
                name="new-userName"
                className="pr-8"
                value={values.userName}
                onChange={setField("userName")}
                onBlur={handleUsernameBlur}
                required
              />
              <Button
                type="button"
                variant="ghost"
                size="icon-xs"
                aria-label="Lookup domain user"
                className="absolute top-1/2 right-1 -translate-y-1/2"
                disabled={!values.userName.trim() || isLookingUp}
                onClick={handleLookupClick}
              >
                {isLookingUp ? <Spinner /> : <SearchIcon />}
              </Button>
            </div>
            {lookupFound && (
              <p className="text-sm text-green-600 dark:text-green-400">
                User found on domain.
              </p>
            )}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              name="new-password"
              type="password"
              value={values.password}
              onChange={setField("password")}
              required={values.authProvider !== "AD"}
            />
            {values.authProvider === "AD" && (
              <p className="text-xs text-muted-foreground">
                Password is not required for AD accounts.
              </p>
            )}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="authProvider">Auth provider</Label>
            <Combobox
              id="authProvider"
              name="authProvider"
              value={values.authProvider}
              onValueChange={(value) =>
                setValues((previous) => ({ ...previous, authProvider: value }))
              }
              options={AUTH_PROVIDER_SELECT_OPTIONS}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="firstName">First name</Label>
              <Input
                id="firstName"
                name="firstName"
                value={values.firstName}
                onChange={setField("firstName")}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="lastName">Last name</Label>
              <Input
                id="lastName"
                name="lastName"
                value={values.lastName}
                onChange={setField("lastName")}
              />
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              name="email"
              type="email"
              value={values.email}
              onChange={setField("email")}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="phoneNumber">Phone number</Label>
            <Input
              id="phoneNumber"
              name="phoneNumber"
              value={values.phoneNumber}
              onChange={setField("phoneNumber")}
            />
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" loading={pending}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
