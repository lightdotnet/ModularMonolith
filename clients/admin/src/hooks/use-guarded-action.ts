"use client";

import { useTransition } from "react";
import { notifyError, notifySuccess } from "@/components/toast";

interface ActionResult {
  error?: string;
  success?: boolean;
}

/** Centralizes the "run an action, toast the result, track pending" pattern used by imperative (non-form) calls. */
export function useGuardedAction() {
  const [pending, startTransition] = useTransition();

  function run(
    action: () => Promise<ActionResult>,
    successMessage?: string,
    onSuccess?: () => void,
  ) {
    startTransition(async () => {
      const result = await action();

      if (!result.success) {
        notifyError(result.error || "Something went wrong.");
        return;
      }

      if (successMessage) notifySuccess(successMessage);
      onSuccess?.();
    });
  }

  return [pending, run] as const;
}
