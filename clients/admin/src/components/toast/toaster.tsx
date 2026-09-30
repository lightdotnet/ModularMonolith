"use client";

import { useTheme } from "next-themes";
import { Toaster } from "sonner";
import { saturatedToastOptions } from "@/components/toast/toast-theme";

export function AppToaster() {
  const { resolvedTheme } = useTheme();

  return (
    <Toaster
      theme={resolvedTheme === "dark" ? "dark" : "light"}
      position="top-right"
      closeButton
      toastOptions={saturatedToastOptions}
      style={{ zIndex: "var(--z-index-toast)" }}
    />
  );
}
