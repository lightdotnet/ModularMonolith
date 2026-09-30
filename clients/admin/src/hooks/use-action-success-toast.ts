"use client";

import { useEffect } from "react";
import { notifySuccess } from "@/components/toast";

interface ActionResult {
  error?: string;
  success?: boolean;
}

/** Toasts and runs `onSuccess` once when a `useActionState` result turns successful. */
export function useActionSuccessToast(
  state: ActionResult,
  successMessage: string,
  onSuccess?: () => void,
) {
  useEffect(() => {
    if (!state.success) return;
    notifySuccess(successMessage);
    onSuccess?.();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only react to a fresh success result
  }, [state.success]);
}
