"use client";

import { useEffect, useRef, useState } from "react";
import { Check, Copy, Eye, EyeOff } from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { notifyError, notifySuccess } from "@/components/toast";

const COPIED_RESET_MS = 2000;
const MASKED_VALUE = "•".repeat(64);

function TokenRow({ label, value }: { label: string; value: string | null }) {
  const [copied, setCopied] = useState(false);
  const [revealed, setRevealed] = useState(false);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  const hasValue = Boolean(value);

  async function handleCopy() {
    if (!value) return;

    if (!navigator?.clipboard) {
      notifyError("Clipboard not available");
      return;
    }

    try {
      await navigator.clipboard.writeText(value);
      notifySuccess("Copied to clipboard");
      setCopied(true);
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
      timeoutRef.current = setTimeout(() => setCopied(false), COPIED_RESET_MS);
    } catch {
      notifyError("Failed to copy");
    }
  }

  return (
    <div className="rounded-2xl border border-border bg-muted/30 p-4">
      <div className="flex items-center justify-between gap-3">
        <span className="text-sm font-medium text-muted-foreground">{label}</span>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={!hasValue}
            onClick={() => setRevealed((prev) => !prev)}
            aria-label={revealed ? "Hide token" : "Show token"}
          >
            {revealed ? <EyeOff /> : <Eye />}
          </Button>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={!hasValue}
            onClick={handleCopy}
          >
            {copied ? <Check /> : <Copy />}
          </Button>
        </div>
      </div>
      <p className="mt-2 max-h-32 overflow-y-auto font-mono text-xs break-all text-muted-foreground">
        {revealed ? value : hasValue ? MASKED_VALUE : null}
      </p>
    </div>
  );
}

export function SessionTokensCard({
  accessToken,
  refreshToken,
}: {
  accessToken: string;
  refreshToken: string | null;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Session tokens</CardTitle>
        <CardDescription>
          Raw tokens for the current sign-in. Visible to super admins only.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex flex-col gap-4">
          <TokenRow label="Access token" value={accessToken} />
          <TokenRow label="Refresh token" value={refreshToken} />
        </div>
      </CardContent>
    </Card>
  );
}
