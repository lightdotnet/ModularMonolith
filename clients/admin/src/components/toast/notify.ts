import { toast } from "sonner";

export function notifySuccess(message: string): void {
  if (message) toast.success(message);
}

export function notifyError(message: string): void {
  toast.error(message);
}
