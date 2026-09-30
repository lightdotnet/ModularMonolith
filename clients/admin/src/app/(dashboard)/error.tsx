"use client";

import { useEffect, useState } from "react";
import { AlertTriangle } from "lucide-react";
import {
  Alert,
  AlertTitle,
  AlertDescription,
  AlertAction,
} from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { DeploymentRecoveryNotice } from "@/components/layout/deployment-recovery-notice";
import {
  isRecoverableDeploymentError,
  peekDeploymentRecoveryExhausted,
  runDeploymentRecovery,
} from "@/lib/shared/deployment-recovery";

export default function DashboardError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const recoverable = isRecoverableDeploymentError(error);
  // Seed from the counter at mount, before recovery bumps it.
  const [recoveryExhausted, setRecoveryExhausted] = useState(
    () => recoverable && peekDeploymentRecoveryExhausted(),
  );

  useEffect(() => {
    if (!recoverable) {
      console.error(error);
      return;
    }
    if (recoveryExhausted) return;
    return runDeploymentRecovery(() => setRecoveryExhausted(true));
  }, [error, recoverable, recoveryExhausted]);

  if (recoverable) {
    return (
      <div className="py-12">
        <DeploymentRecoveryNotice exhausted={recoveryExhausted} />
      </div>
    );
  }

  return (
    <div className="py-12">
      <Alert variant="destructive" className="mx-auto max-w-md">
        <AlertTriangle />
        <AlertTitle>Something went wrong</AlertTitle>
        <AlertDescription>
          {error.message || "An unexpected error occurred while loading this page."}
        </AlertDescription>
        <AlertAction>
          <Button size="sm" variant="outline" onClick={reset}>
            Try again
          </Button>
        </AlertAction>
      </Alert>
    </div>
  );
}
