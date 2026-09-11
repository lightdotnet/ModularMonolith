"use client";

import { useActionState, useState } from "react";
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
import { Textarea } from "@/components/ui/textarea";
import { useActionSuccessToast } from "@/hooks/use-action-success-toast";
import {
  sendNotificationAction,
  type SendNotificationFormState,
} from "@/modules/notifications/api/send-notification-action";
import { UserSelect } from "@/modules/notifications/components/user-select";
import { getDisplayName } from "@/lib/shared/user-display";

const initialState: SendNotificationFormState = {};

interface FormValues {
  fromUserId: string;
  fromName: string;
  toUserId: string;
  title: string;
  message: string;
  url: string;
}

const initialValues: FormValues = {
  fromUserId: "",
  fromName: "",
  toUserId: "",
  title: "",
  message: "",
  url: ""
};

interface SendNotificationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSent: () => void;
}

export function SendNotificationDialog({
  open,
  onOpenChange,
  onSent,
}: SendNotificationDialogProps) {
  const [state, formAction, pending] = useActionState(sendNotificationAction, initialState);
  const [values, setValues] = useState<FormValues>(initialValues);

  function setField(field: keyof FormValues) {
    return (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
      setValues((previous) => ({ ...previous, [field]: event.target.value }));
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setValues(initialValues);
    onOpenChange(nextOpen);
  }

  useActionSuccessToast(state, "Notification sent.", () => {
    onSent();
    onOpenChange(false);
    setValues(initialValues);
  });

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent onPointerDownOutside={(event) => event.preventDefault()}>
        <form action={formAction} className="flex flex-col gap-4">
          <DialogHeader>
            <DialogTitle>Send notification</DialogTitle>
          </DialogHeader>

          {state.error && (
            <Alert variant="destructive">
              <AlertDescription>{state.error}</AlertDescription>
            </Alert>
          )}

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="fromUserId">From</Label>
            <input type="hidden" name="fromName" value={values.fromName} />
            <UserSelect
              id="fromUserId"
              name="fromUserId"
              value={values.fromUserId}
              onValueChange={(user) =>
                setValues((previous) => ({
                  ...previous,
                  fromUserId: user.id,
                  fromName: getDisplayName(user),
                }))
              }
              triggerClassName="w-full"
              placeholder="Send as system or select a user"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="toUserId">Recipient</Label>
            <UserSelect
              id="toUserId"
              name="toUserId"
              value={values.toUserId}
              onValueChange={(user) =>
                setValues((previous) => ({ ...previous, toUserId: user.id }))
              }
              triggerClassName="w-full"
              placeholder="Select a user"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="title">Title</Label>
            <Input
              id="title"
              name="title"
              value={values.title}
              onChange={setField("title")}
              required
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="message">Message</Label>
            <Textarea
              id="message"
              name="message"
              value={values.message}
              onChange={setField("message")}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="url">Link URL</Label>
            <Input id="url" name="url" value={values.url} onChange={setField("url")} />
          </div>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" loading={pending} disabled={!values.toUserId}>
              Send
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
