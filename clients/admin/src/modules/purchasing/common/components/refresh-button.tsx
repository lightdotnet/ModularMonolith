"use client";

import { useTransition } from "react";
import { useRouter } from "next/navigation";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";

/** Re-runs the server page, used while a stock movement is still being finalized. */
export function RefreshButton() {
  const router = useRouter();
  const [pending, startTransition] = useTransition();

  return (
    <Button size="sm" variant="outline" loading={pending} onClick={() => startTransition(() => router.refresh())}>
      <RefreshCw className="size-4" />
      Refresh
    </Button>
  );
}
